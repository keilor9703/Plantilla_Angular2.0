<#
=============================================================================
 Prueba end-to-end del login por QR en SI2FA, simulando el celular (PowerShell).
 Hace de las DOS partes (navegador SI2FA + celular). No requiere openssl ni jq:
 usa el cifrado nativo de .NET (ECDSA P-256).

 Uso (PowerShell o pwsh):
   .\qr_e2e_simular_celular.ps1 -Api2faUrl https://localhost:44382 `
       -Si2faUrl http://localhost:5100 -Usuario ke.martinez -Identificacion 123418818

 Prerrequisitos del servidor:
   - Api2FA con QrLogin:Enabled=true y QrLogin:PermitirRegistroPoc=true
   - SI2FA con Mfa:ApiBaseUrl/HmacSecret y QrLogin:Enabled=true; SI2FA en mfa_sistemas
   NOTA: -Si2faUrl debe apuntar al BACKEND de SI2FA (donde vive /api/Cuenta).
         Si usas el dev server de Angular (puerto 4300), funciona si su proxy
         reenvía /api al backend.
=============================================================================
#>
param(
  [string]$Api2faUrl = "https://localhost:44382",
  [string]$Si2faUrl  = "http://localhost:5100",
  [string]$Usuario   = "jperez",
  [long]  $Identificacion = 1234188418
)

$ErrorActionPreference = "Stop"
function Say($m){ Write-Host "`n== $m" -ForegroundColor Cyan }
function Die($m){ Write-Host "`nERROR: $m" -ForegroundColor Red; exit 1 }

# --- Ignorar certificado self-signed (IIS Express) en 5.1 y en 7 ----------------
$script:IrmExtra = @{}
if ($PSVersionTable.PSVersion.Major -ge 6) {
  $script:IrmExtra["SkipCertificateCheck"] = $true
} else {
  [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}
function Api($method, $url, $headers, $body) {
  $a = @{ Method = $method; Uri = $url; Headers = $headers } + $script:IrmExtra
  if ($body) { $a["Body"] = $body; $a["ContentType"] = "application/json" }
  return Invoke-RestMethod @a
}

# --- Cifrado: ECDSA P-256 (llave del dispositivo), SPKI + firma DER -------------
$ec = [System.Security.Cryptography.ECDsa]::Create(
        [System.Security.Cryptography.ECCurve]::CreateFromFriendlyName("nistP256"))

function Get-PublicKeySpkiB64 {
  $p = $ec.ExportParameters($false)
  $x = New-Object byte[] 32; $y = New-Object byte[] 32
  [Array]::Copy($p.Q.X, 0, $x, 32 - $p.Q.X.Length, $p.Q.X.Length)
  [Array]::Copy($p.Q.Y, 0, $y, 32 - $p.Q.Y.Length, $p.Q.Y.Length)
  $header = [byte[]](0x30,0x59,0x30,0x13,0x06,0x07,0x2A,0x86,0x48,0xCE,0x3D,0x02,0x01,
                     0x06,0x08,0x2A,0x86,0x48,0xCE,0x3D,0x03,0x01,0x07,0x03,0x42,0x00)
  $spki = New-Object System.Collections.Generic.List[byte]
  $spki.AddRange($header); $spki.Add(0x04); $spki.AddRange($x); $spki.AddRange($y)
  return [Convert]::ToBase64String($spki.ToArray())
}

function ConvertTo-DerInteger([byte[]]$v) {
  $i = 0; while ($i -lt $v.Length - 1 -and $v[$i] -eq 0) { $i++ }
  $v = $v[$i..($v.Length-1)]
  if ($v[0] -band 0x80) { $v = ,([byte]0) + $v }
  return ,([byte]0x02) + ,([byte]$v.Length) + $v
}
function Sign([string]$msg) {
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($msg)
  # SignData devuelve IEEE P1363 (r||s, 64 bytes); lo convertimos a DER/X9.62.
  $p1363 = $ec.SignData($bytes, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
  $r = ConvertTo-DerInteger ($p1363[0..31])
  $s = ConvertTo-DerInteger ($p1363[32..63])
  $seq = ,([byte]0x30) + ,([byte]($r.Length + $s.Length)) + $r + $s
  return [Convert]::ToBase64String([byte[]]$seq)
}

$h = @{ }

# 1) Llave del dispositivo
Say "1) Llave del dispositivo (ECDSA P-256)"
$PUB = Get-PublicKeySpkiB64

# 2) Registrar dispositivo (PoC)
Say "2) Registrando dispositivo (PoC) en Api2FA"
$reg = Api POST "$Api2faUrl/api/Identidad/RegistrarDispositivoPoc" $h (@{
  Identificacion = $Identificacion; Usuario = $Usuario; PublicKey = $PUB
  Alias = "Simulador"; Plataforma = "windows"; Modelo = "powershell"
} | ConvertTo-Json)
$DEV = $reg.Data.IdDispositivo
if (-not $DEV) { Die "No se registró el dispositivo. Resp: $($reg | ConvertTo-Json -Depth 5)  (¿PermitirRegistroPoc=true?)" }
Write-Host "   IdDispositivo = $DEV"

# 3) Bearer de dispositivo
Say "3) Bearer de dispositivo (reto-respuesta)"
$non = Api POST "$Api2faUrl/api/Identidad/DeviceNonce" $h (@{ IdDispositivo = $DEV } | ConvertTo-Json)
$SNONCE = $non.Data.ServerNonce
if (-not $SNONCE) { Die "No se obtuvo nonce. Resp: $($non | ConvertTo-Json -Depth 5)" }
$sigd = Sign "DEVICE-AUTH|v1|$DEV|$SNONCE"
$tok = Api POST "$Api2faUrl/api/Identidad/DeviceToken" $h (@{ IdDispositivo = $DEV; ServerNonce = $SNONCE; Firma = $sigd } | ConvertTo-Json)
$DEVBEARER = $tok.Data.DeviceBearer
if (-not $DEVBEARER) { Die "No se obtuvo bearer. Resp: $($tok | ConvertTo-Json -Depth 5)" }
Write-Host "   bearer OK"

# 4) [Navegador SI2FA] Crear sesión QR
Say "4) [Navegador SI2FA] Creando sesión QR"
$qc = Api POST "$Si2faUrl/api/Cuenta/QrCreate" $h "{}"
$SID = $qc.sid; $WEBTOKEN = $qc.webPollToken; $PAIRING = $qc.pairing
if (-not $SID) { Die "SI2FA no creó la sesión. Resp: $($qc | ConvertTo-Json -Depth 5)" }
Write-Host "   sid=$SID  pairing=$PAIRING"

# 5) [Celular] Obtener el reto
Say "5) [Celular] Obteniendo el reto (Challenge)"
$ch = Api GET "$Api2faUrl/api/Qr/Challenge?sid=$SID" @{ Authorization = "Bearer $DEVBEARER" } $null
$CHALLENGE = $ch.Data.Challenge; $SISTEMA = $ch.Data.Sistema
if (-not $CHALLENGE) { Die "No se obtuvo el reto. Resp: $($ch | ConvertTo-Json -Depth 5)" }
Write-Host "   sistema=$SISTEMA  (número a verificar: $PAIRING)"

# 6) [Celular] Aprobar firmando el reto
Say "6) [Celular] Aprobando (firma ECDSA)"
$siga = Sign "QR-APPROVE|v1|$SID|$CHALLENGE|$SISTEMA|$DEV"
$ap = Api POST "$Api2faUrl/api/Qr/Approve" @{ Authorization = "Bearer $DEVBEARER" } (@{ Sid = $SID; Firma = $siga } | ConvertTo-Json)
if ($ap.CodigoExito -ne 1) { Die "La aprobación falló. Resp: $($ap | ConvertTo-Json -Depth 5)" }
Write-Host "   aprobado"

# 7) [Navegador SI2FA] Polling → entrar
Say "7) [Navegador SI2FA] Polling de estado"
for ($i = 1; $i -le 10; $i++) {
  $st = Api GET "$Si2faUrl/api/Cuenta/QrStatus?sid=$SID&webPollToken=$([uri]::EscapeDataString($WEBTOKEN))" $h $null
  Write-Host "   intento $i -> estado=$($st.estado)"
  if ($st.estado -eq "APPROVED" -and $st.token) {
    Write-Host "`n[OK] LOGIN POR QR EXITOSO en SI2FA" -ForegroundColor Green
    Write-Host "JWT de sesión SI2FA:"; Write-Host $st.token
    exit 0
  }
  if ($st.estado -in @("EXPIRED","REJECTED","SVCDOWN","ERROR")) { Die "Estado terminal: $($st.estado) ($($st.message))" }
  Start-Sleep -Seconds 1
}
Die "No se completó el login (timeout de polling)."
