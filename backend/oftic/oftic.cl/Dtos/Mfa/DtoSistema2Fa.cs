using System;

namespace Comun.Dtos.Mfa
{
    /// <summary>
    /// Sistema de información integrado al servicio de Doble Factor de Autenticación (SI2FA).
    /// Reemplaza la configuración estática "SistemasConfiables" de appsettings por una tabla
    /// administrable (mfa_sistemas). El secreto HMAC NUNCA se devuelve en las consultas de
    /// lectura; solo se muestra una vez al crearlo o al rotarlo.
    /// </summary>
    public class DtoSistema2Fa
    {
        public long IdSistema { get; set; }
        public string Sigla { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Habilitado { get; set; }
        /// <summary>Vista enmascarada del secreto (p. ej. "••••••••abcd"). Nunca el valor real.</summary>
        public string? SecretoEnmascarado { get; set; }
        public DateTime? CreadoAt { get; set; }
        public DateTime? ActualizadoAt { get; set; }
        public DateTime? RotadoAt { get; set; }
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModifica { get; set; }
    }

    public class DtoSistema2FaRequest
    {
        public string Sigla { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Habilitado { get; set; } = 1;
    }

    public class DtoSistema2FaResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long Id { get; set; }
        /// <summary>
        /// Secreto HMAC en claro. Solo se entrega UNA vez (al crear o rotar). No se persiste
        /// para mostrarse de nuevo; el administrador debe copiarlo y entregarlo al sistema integrado.
        /// </summary>
        public string? Secreto { get; set; }
    }
}
