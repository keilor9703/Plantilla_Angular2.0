# Prueba end-to-end — Login por QR en SI2FA desde el celular

Flujo objetivo: abres SI2FA en el PC → "Ingresar con QR" → escaneas con la app →
confirmas con biometría → **entras a SI2FA sin contraseña**.

Participan 3 piezas: **Api2FA** (motor), **SI2FA** (web que muestra el QR) y la **app móvil**.

---

## 0. Prerrequisitos

**Base de datos (PostgreSQL, esquema `usr_twoaut`)** — correr en el repo Api2FA:
- `docs/sql/qr_passwordless_poc.sql`
- `docs/sql/qr_enrolamiento_fase1.sql`
- `docs/sql/qr_device_session_fase2.sql`

**SI2FA registrado como sistema consumidor:** debe existir en `mfa_sistemas` con una
**sigla `SI2FA`** y un **secreto HMAC**. Anota ese secreto (lo usan Api2FA y SI2FA).

**Red:** el celular debe **alcanzar la URL de Api2FA**. En pruebas, lo más simple es HTTP
en la misma red (evitas el tema de certificados). El PC con SI2FA también debe alcanzar Api2FA.

---

## 1. Configuración

**Api2FA** (`ApiSECAD/appsettings.json`):
```json
"QrLogin": { "Enabled": true, "PermitirRegistroPoc": true }
```
(`PermitirRegistroPoc=true` solo para esta prueba, para enrolar sin PIP; en producción se usa el correo.)

**SI2FA** (`backend/oftic/oftic/appsettings.json`):
```json
"Mfa":      { "Enabled": false, "ApiBaseUrl": "http://IP_API2FA:PUERTO", "Sistema": "SI2FA", "HmacSecret": "<EL_SECRETO_HMAC_DE_SI2FA>" },
"QrLogin":  { "Enabled": true }
```
> `Mfa:Enabled=false` deja el login por contraseña **sin** 2FA TOTP, para no mezclar flujos en la prueba.
> El QR funciona igual porque usa su propia ruta. `Mfa:ApiBaseUrl`/`HmacSecret` SÍ se usan para el QR.

**App** (`Api2FA-App/src/config.ts`): `apiBaseUrl: 'http://IP_API2FA:PUERTO/api'`.

Levanta **Api2FA** y **SI2FA** (`dotnet run` / IIS; `ng serve` o build del frontend).

---

## 2. Enrolar el celular (una vez)

Necesitas un dispositivo registrado con su llave. Dos caminos:

**2.a — Con la app (real):** compila la app en un **celular real** (ver `Api2FA-App/README.md`),
e implementa la pantalla de enrolamiento (token + código). Requiere el broker/PIP para el código.

**2.b — Rápido para la prueba (sin app aún / sin PIP):** registra un dispositivo con `curl`
siguiendo `Api2FA/docs/QR_POC_PRUEBAS.md` (pasos 1 y 2.b, con `PermitirRegistroPoc=true`).
Guarda el `IdDispositivo` y la llave privada `dev.key` para firmar.

> La identidad del dispositivo (su `usuario_institucional`) debe existir también como usuario
> en SI2FA (haber entrado antes por contraseña), para que SI2FA le cargue sus roles.

---

## 3. Probar el login por QR

> **Atajo:** `docs/qr_e2e_simular_celular.sh` hace TODO el flujo automáticamente (simula el
> celular y el navegador con `curl`): registra un dispositivo, obtiene el bearer, crea la
> sesión en SI2FA, aprueba firmando y confirma el login. Uso:
> `API2FA_URL=http://IP:PUERTO SI2FA_URL=http://IP:PUERTO USUARIO=jperez IDENTIFICACION=123 ./docs/qr_e2e_simular_celular.sh`
> (requiere `QrLogin:PermitirRegistroPoc=true` en Api2FA). Lo de abajo es el paso a paso manual.


1. Abre **SI2FA** en el navegador → pantalla de login → botón **"Ingresar con QR"**.
   - SI2FA llama a `Api2FA /api/Qr/CreateSession` (firmando con el HMAC de SI2FA) y muestra el
     **QR** + el **número de verificación**, y empieza a hacer *polling*.
2. **Aprueba desde el dispositivo:**
   - **Con la app:** escanea → la app pide el bearer (`DeviceNonce`/`DeviceToken`), muestra el
     contexto y el número → apruebas con **biometría** → firma y envía `Qr/Approve`.
   - **Con `curl`** (si no tienes la app lista): sigue `QR_POC_PRUEBAS.md` Fase 2 (D1/D2 para el
     bearer) y pasos 4–5 (Challenge/Approve) usando el `sid` del QR (léelo del `qrPayload`).
3. Al aprobarse, Api2FA marca la sesión `APPROVED` y emite su JWT. SI2FA lo detecta en el
   *polling*, **lo traduce a su propia sesión** (carga los roles del usuario) y **entra a /home**.

---

## 4. Qué validar

- El **número de verificación** del navegador coincide con el de la app (anti-phishing).
- Al aprobar, el navegador entra **sin pedir contraseña**.
- En **SI2FA → Auditoría** aparecen los eventos (`QR_SESSION_CREATED`, `QR_SCANNED`,
  `QR_APPROVED`) con descripción legible.
- Si dejas pasar el TTL (~90 s) sin aprobar, el navegador muestra "código expiró".

---

## 5. Problemas comunes

| Síntoma | Causa probable |
|---|---|
| "Servicio de ingreso por QR no disponible" | `Mfa:ApiBaseUrl` mal, Api2FA caído, o el celular/PC no alcanza Api2FA. |
| `CreateSession` responde 401 | El `HmacSecret` de SI2FA no coincide con el de `mfa_sistemas`, o la hora del servidor está desincronizada (NTP). |
| Aprueba pero SI2FA dice "usuario no habilitado" | El `usuario` del dispositivo no existe en la BD de usuarios de SI2FA. Haz un login por contraseña primero. |
| "Firma inválida" al aprobar | La llave pública registrada no corresponde a la privada que firma, o el formato (SPKI/DER) no coincide. |
| El QR no se ve | Revisa que Api2FA devuelva `qrPngBase64` (requiere el commit de QRCoder). |

---

## 6. Lo que falta para "producción"
- **App compilada y probada en dispositivo real** (la firma por hardware no se prueba en simulador).
- Enrolamiento por **OUD + correo (PIP)** en vez del registro PoC.
- Validación de **attestation** (Play Integrity / App Attest).
- Despliegue de Api2FA en los 2 nodos con **NTP** y **DataProtection** compartido.
