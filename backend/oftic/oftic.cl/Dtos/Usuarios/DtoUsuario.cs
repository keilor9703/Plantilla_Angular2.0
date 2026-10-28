using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Comun.Dtos
{
    public class DtoFuncionario
    {
        public string? identificacion { get; set; }
        public string? nombres { get; set; }
        public string? apellidos { get; set; }
        public string? situacionLaboral { get; set; }
        public string? correo { get; set; }
        public string? usuario { get; set; }
        public string? celular { get; set; }
        public string? dependencia { get; set; }
        public string? siglaFisica { get; set; }
        public string? siglaLaborando { get; set; }
        public string? siglaDependencia { get; set; }
        public string? nombreGrado { get; set; }
        public string? cargo { get; set; }
        public string? tiempoServicio { get; set; }
        public string? gradAlfabetico { get; set; }
        public string? funcionario { get; set; }
        public string? funcionarioCodigo { get; set; }
        public string? undeLaborandoCodigo { get; set; }
        public string? codigoCargo { get; set; }
        public long? idCategoria { get; set; }
        public string? categoriaDescripcion { get; set; }
        public bool activo { get; set; } = true;
    }

    public class DtoRol
    {
        public int id { get; set; }
        public string? nombre { get; set; }
    }

    public class DtoRolAsignado
    {
        public int id { get; set; }
        public string? rol { get; set; }
        public string? fechaInicio { get; set; }
        public string? fechaFin { get; set; }
        public string? estado { get; set; }
        public string? justificacion { get; set; }
    }

    public class DtoUsuarioListadoItem
    {
        public long idUsuario { get; set; }
        public string identificacion { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string nombreCompleto { get; set; } = string.Empty;
        public string rol { get; set; } = string.Empty;
        public string? fechaFinRol { get; set; }
    }

    public class DtoTokenRequest
    {
        public string? Usuario { get; set; }
        public string? Contrasena { get; set; }
        /// <summary>Identificador del dispositivo (navegador) para el reconocimiento de dispositivos confiables.</summary>
        public string? DeviceId { get; set; }
    }

    public class DtoImpersonateRequest
    {
        public string? Usuario { get; set; }
    }

    public class DtoRefreshTokenRequest
    {
        public string? Token { get; set; }
    }

    public class DtoTokenResponse
    {
        public string? token { get; set; }
        public string? usuario { get; set; }
        public bool success { get; set; }
        public string? message { get; set; }
        public bool impersonating { get; set; }
        public string? impersonatedBy { get; set; }

        // --- Segundo factor (SI2FA) ---
        /// <summary>True cuando, tras validar la contraseña, se requiere el segundo factor.</summary>
        public bool requiere2Fa { get; set; }
        /// <summary>True cuando el usuario debe enrolar su segundo factor (primera vez).</summary>
        public bool enrolar { get; set; }
        /// <summary>Challenge cifrado que vincula el paso 1 (contraseña) con el paso 2 (código). No es un token de acceso.</summary>
        public string? challenge { get; set; }
        /// <summary>Código QR (base64) para el enrolamiento.</summary>
        public string? qr { get; set; }
        /// <summary>Clave manual (texto) para el enrolamiento.</summary>
        public string? claveManual { get; set; }
        /// <summary>Token de enrolamiento temporal devuelto por el servicio 2FA.</summary>
        public string? enrollToken { get; set; }
    }

    public class DtoUsuarioRequest
    {
        public string? username { get; set; }
        public string? identificacion { get; set; }
        public string? nombres { get; set; }
        public string? apellidos { get; set; }
        public string? email { get; set; }
        public string? gradAlfabetico { get; set; }
        public string? funcionario { get; set; }
        public string? undeLaborando { get; set; }
        public string? codigoCargo { get; set; }
        public bool activo { get; set; }
    }

    public class DtoGuardarUsuarioResult
    {
        public long idUsuario { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class DtoAsignarRolRequest
    {
        public int usuarioId { get; set; }
        public string? usuario { get; set; }
        public string? identificacion { get; set; }
        public int rolId { get; set; }
        public string? justificacion { get; set; }
        public string? fechaFin { get; set; }
        public int vigente { get; set; } = 1;
    }

    public class DtoEditarRolRequest
    {
        public string? usuario { get; set; }
        public string? identificacion { get; set; }
        public string? justificacion { get; set; }
        public string? fechaFin { get; set; }
    }

    public class DtoRolAdmin
    {
        public int id { get; set; }
        public string? nombre { get; set; }
        public int vigente { get; set; }
    }

    public class DtoSaveRolRequest
    {
        public int? id { get; set; }
        public string? nombre { get; set; }
        public int vigente { get; set; } = 1;
    }

    public class DtoSetRolEstadoRequest
    {
        public int vigente { get; set; }
        public string? observaciones { get; set; }
    }

    public class DtoSaveRolResult
    {
        public long idRol { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
