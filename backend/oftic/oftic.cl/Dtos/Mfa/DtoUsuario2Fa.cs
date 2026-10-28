using System;
using System.Collections.Generic;

namespace Comun.Dtos.Mfa
{
    /// <summary>Usuario registrado en el servicio de doble autenticación (tabla mfa_usuarios).</summary>
    public class DtoUsuario2Fa
    {
        public long IdMfa { get; set; }
        public long Identificacion { get; set; }
        public string UsuarioInstitucional { get; set; } = string.Empty;
        public int MfaHabilitado { get; set; }
        public int RequireReenroll { get; set; }
        public bool Enrolado { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueoHasta { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime? UltimoOk { get; set; }
        public int DispositivosConfiables { get; set; }
        public DateTime? CreadoAt { get; set; }
        public DateTime? ActualizadoAt { get; set; }
    }

    public class DtoUsuario2FaDetalle : DtoUsuario2Fa
    {
        public List<DtoDispositivo2Fa> Dispositivos { get; set; } = new();
    }

    public class DtoDispositivo2Fa
    {
        public long IdDevice { get; set; }
        public long IdMfa { get; set; }
        public string? DeviceHash { get; set; }
        public DateTime? ExpiraAt { get; set; }
        public bool Vigente { get; set; }
        public DateTime? CreadoAt { get; set; }
        // Datos del propietario (para el módulo de dispositivos)
        public long Identificacion { get; set; }
        public string? UsuarioInstitucional { get; set; }
    }

    /// <summary>Resultado genérico de una acción administrativa sobre un usuario 2FA.</summary>
    public class DtoAccion2FaResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>Petición para habilitar/deshabilitar el MFA de un usuario.</summary>
    public class DtoCambioEstado2FaRequest
    {
        public int Estado { get; set; } // 1 = habilitar, 0 = deshabilitar
        public string? Observaciones { get; set; }
    }
}
