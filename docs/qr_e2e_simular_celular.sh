#!/usr/bin/env bash
# =============================================================================
# Prueba end-to-end del login por QR en SI2FA, simulando el celular con curl.
#
# Hace de las DOS partes:
#   - "Navegador SI2FA": crea la sesión QR y hace polling hasta entrar.
#   - "Celular": registra un dispositivo, obtiene el bearer, escanea y aprueba.
#
# Resultado esperado: al final imprime el JWT de sesión de SI2FA (login logrado).
#
# Requisitos: bash, curl, openssl, jq.
# Prerrequisitos del servidor:
#   - Api2FA con QrLogin:Enabled=true y QrLogin:PermitirRegistroPoc=true
#   - Migraciones corridas; SI2FA registrado en mfa_sistemas con su secreto HMAC
#   - SI2FA con Mfa:ApiBaseUrl/HmacSecret y QrLogin:Enabled=true
# =============================================================================
set -euo pipefail

# ----------------------------- Configuración ---------------------------------
API2FA_URL="${API2FA_URL:-http://localhost:5224}"      # URL base de Api2FA
SI2FA_URL="${SI2FA_URL:-http://localhost:5100}"        # URL base del backend de SI2FA
USUARIO="${USUARIO:-jperez}"                           # usuario institucional del dispositivo
IDENTIFICACION="${IDENTIFICACION:-1234188418}"         # cédula del funcionario
CURL="curl -sk"                                        # -k: ignora cert en pruebas

for dep in curl openssl jq; do command -v "$dep" >/dev/null || { echo "Falta: $dep"; exit 1; }; done

WORK="$(mktemp -d)"; trap 'rm -rf "$WORK"' EXIT
KEY="$WORK/dev.key"

say() { printf '\n\033[1;36m== %s\033[0m\n' "$*"; }
die() { printf '\n\033[1;31mERROR: %s\033[0m\n' "$*"; exit 1; }
# jq con respaldo PascalCase/camelCase
jqv() { jq -r "$1 // empty"; }

# --------------------- 1. Llave del dispositivo (ECDSA P-256) -----------------
say "1) Generando llave del dispositivo (ECDSA P-256)"
openssl ecparam -name prime256v1 -genkey -noout -out "$KEY"
PUB=$(openssl ec -in "$KEY" -pubout -outform DER 2>/dev/null | base64 -w0)
firmar() { printf '%s' "$1" | openssl dgst -sha256 -sign "$KEY" | base64 -w0; }

# --------------------- 2. Registrar el dispositivo (PoC) ----------------------
say "2) Registrando dispositivo (PoC) en Api2FA"
REG=$($CURL -X POST "$API2FA_URL/api/Identidad/RegistrarDispositivoPoc" \
  -H "Content-Type: application/json" \
  -d "{\"Identificacion\":$IDENTIFICACION,\"Usuario\":\"$USUARIO\",\"PublicKey\":\"$PUB\",\"Alias\":\"Simulador\",\"Plataforma\":\"android\",\"Modelo\":\"curl\"}")
DEV=$(echo "$REG" | jq -r '.Data.idDispositivo // .Data.IdDispositivo // empty')
[ -n "$DEV" ] || die "No se registró el dispositivo. Respuesta: $REG  (¿PermitirRegistroPoc=true?)"
echo "   IdDispositivo = $DEV"

# --------------------- 3. Bearer de dispositivo (reto-respuesta) --------------
say "3) Obteniendo bearer de dispositivo"
NON=$($CURL -X POST "$API2FA_URL/api/Identidad/DeviceNonce" -H "Content-Type: application/json" -d "{\"IdDispositivo\":$DEV}")
SNONCE=$(echo "$NON" | jq -r '.Data.serverNonce // .Data.ServerNonce // empty')
[ -n "$SNONCE" ] || die "No se obtuvo nonce. Respuesta: $NON"
SIGD=$(firmar "DEVICE-AUTH|v1|$DEV|$SNONCE")
TOK=$($CURL -X POST "$API2FA_URL/api/Identidad/DeviceToken" -H "Content-Type: application/json" \
  -d "{\"IdDispositivo\":$DEV,\"ServerNonce\":\"$SNONCE\",\"Firma\":\"$SIGD\"}")
DEVBEARER=$(echo "$TOK" | jq -r '.Data.deviceBearer // .Data.DeviceBearer // empty')
[ -n "$DEVBEARER" ] || die "No se obtuvo bearer. Respuesta: $TOK"
echo "   bearer OK"

# --------------------- 4. (Navegador) Crear sesión QR en SI2FA ---------------
say "4) [Navegador SI2FA] Creando sesión QR"
QC=$($CURL -X POST "$SI2FA_URL/api/Cuenta/QrCreate" -H "Content-Type: application/json" -d '{}')
SID=$(echo "$QC" | jq -r '.sid // empty')
WEBTOKEN=$(echo "$QC" | jq -r '.webPollToken // empty')
PAIRING=$(echo "$QC" | jq -r '.pairing // empty')
[ -n "$SID" ] || die "SI2FA no creó la sesión QR. Respuesta: $QC"
echo "   sid=$SID  pairing=$PAIRING"

# --------------------- 5. (Celular) Escanear: obtener el reto -----------------
say "5) [Celular] Obteniendo el reto (Challenge)"
CH=$($CURL "$API2FA_URL/api/Qr/Challenge?sid=$SID" -H "Authorization: Bearer $DEVBEARER")
CHALLENGE=$(echo "$CH" | jq -r '.Data.challenge // .Data.Challenge // empty')
SISTEMA=$(echo "$CH" | jq -r '.Data.sistema // .Data.Sistema // empty')
[ -n "$CHALLENGE" ] || die "No se obtuvo el reto. Respuesta: $CH"
echo "   sistema=$SISTEMA  (número a verificar: $PAIRING)"

# --------------------- 6. (Celular) Aprobar firmando el reto ------------------
say "6) [Celular] Aprobando (firma ECDSA del reto)"
SIGA=$(firmar "QR-APPROVE|v1|$SID|$CHALLENGE|$SISTEMA|$DEV")
AP=$($CURL -X POST "$API2FA_URL/api/Qr/Approve" -H "Content-Type: application/json" \
  -H "Authorization: Bearer $DEVBEARER" -d "{\"Sid\":\"$SID\",\"Firma\":\"$SIGA\"}")
OKAP=$(echo "$AP" | jq -r '.CodigoExito // 0')
[ "$OKAP" = "1" ] || die "La aprobación falló. Respuesta: $AP"
echo "   aprobado"

# --------------------- 7. (Navegador) Polling → entrar a SI2FA ----------------
say "7) [Navegador SI2FA] Consultando estado (polling)"
for i in $(seq 1 10); do
  ST=$($CURL "$SI2FA_URL/api/Cuenta/QrStatus?sid=$SID&webPollToken=$WEBTOKEN")
  ESTADO=$(echo "$ST" | jq -r '.estado // empty')
  TOKEN=$(echo "$ST" | jq -r '.token // empty')
  echo "   intento $i → estado=$ESTADO"
  if [ "$ESTADO" = "APPROVED" ] && [ -n "$TOKEN" ]; then
    printf '\n\033[1;32m✔ LOGIN POR QR EXITOSO en SI2FA\033[0m\n'
    echo "JWT de sesión SI2FA:"; echo "$TOKEN"
    exit 0
  fi
  case "$ESTADO" in EXPIRED|REJECTED|SVCDOWN|ERROR) die "Estado terminal: $ESTADO ($(echo "$ST" | jq -r '.message // ""'))";; esac
  sleep 1
done
die "No se completó el login (timeout de polling)."
