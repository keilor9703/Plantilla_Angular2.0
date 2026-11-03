# SI2FA — Modos de ingreso y configuración (appsettings)

El login tiene **tres interruptores independientes** en `backend/oftic/oftic/appsettings.json`:

| Clave | Qué controla |
|---|---|
| `Auth:BypassOudPasswordValidation` | **Solo en entorno Development.** `true` = NO valida la contraseña contra OUD (pruebas locales sin red institucional). |
| `Mfa:Enabled` | Segundo factor **TOTP** en el login por **contraseña** (modal @policia/mfa). |
| `QrLogin:Enabled` | Muestra el botón **"Ingresar con QR"** (passwordless). Es independiente del login por contraseña. |

Para que **2FA** y **QR** funcionen, además hay que configurar la conexión con Api2FA:
```json
"Mfa": { "ApiBaseUrl": "http://IP_API2FA:PUERTO", "Sistema": "SI2FA", "HmacSecret": "<secreto de mfa_sistemas>" },
"Jwt": { "MfaSessionKey": "<clave larga aleatoria>" }
```
y **SI2FA debe existir en `mfa_sistemas`** con ese mismo `HmacSecret`.

> El entorno se fija con `ASPNETCORE_ENVIRONMENT` (en desarrollo ya viene `Development`
> en `launchSettings.json`). El bypass **se ignora** fuera de Development.

---

## Casos de uso

### 1) Ingresar SIN ninguna validación (solo pruebas locales)
No valida contraseña ni pide 2FA. Entras con cualquier usuario/clave; si el usuario no existe,
se crea.
```json
"Auth":    { "BypassOudPasswordValidation": true },
"Mfa":     { "Enabled": false },
"QrLogin": { "Enabled": false }
```
> ⚠️ **Nunca** `true` en producción. Requiere entorno Development.

### 2) Solo credenciales OUD (sin segundo factor)
Valida usuario/contraseña contra OUD; sin 2FA.
```json
"Auth":    { "BypassOudPasswordValidation": false },
"Mfa":     { "Enabled": false },
"QrLogin": { "Enabled": false }
```
Requiere **red al gateway PIP/OUD** y `ApiSettings.UsuarioPip` / `ClavePip` configurados.

### 3) Credenciales OUD + TOTP 2FA
Tras validar la contraseña, pide el código del autenticador (modal @policia/mfa).
```json
"Auth":    { "BypassOudPasswordValidation": false },
"Mfa":     { "Enabled": true, "ApiBaseUrl": "http://IP_API2FA:PUERTO", "Sistema": "SI2FA", "HmacSecret": "<...>" },
"Jwt":     { "MfaSessionKey": "<clave larga>" },
"QrLogin": { "Enabled": false }
```

### 4) Ingresar con QR (passwordless)
Muestra el botón "Ingresar con QR". Se puede combinar con cualquiera de los anteriores.
```json
"Mfa":     { "ApiBaseUrl": "http://IP_API2FA:PUERTO", "Sistema": "SI2FA", "HmacSecret": "<...>" },
"QrLogin": { "Enabled": true }
```
> El QR **no usa OUD** (la identidad viene del dispositivo), así que funciona aunque el
> login por contraseña no tenga red al gateway.

### 5) Todo a la vez (lo normal en producción)
El funcionario elige: contraseña+2FA **o** QR.
```json
"Auth":    { "BypassOudPasswordValidation": false },
"Mfa":     { "Enabled": true, "ApiBaseUrl": "...", "HmacSecret": "..." },
"QrLogin": { "Enabled": true }
```

---

## Problema típico: "el login por contraseña se queda pensando"
El login por contraseña llama al **OUD/PIP institucional** (`ApiSettings.OudLogin`). Si tu
máquina **no tiene red** a ese host interno (o faltan `UsuarioPip`/`ClavePip`), la llamada se
cuelga hasta el timeout y en el log aparece *"No se agregó Authorization header por token técnico
vacío"*.

- **Pruebas locales:** usa el **caso 1** (bypass) o prueba por **QR** (no toca OUD).
- **Producción:** configura `ApiSettings.UsuarioPip`/`ClavePip` y asegura red al gateway.
