using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Datos.Entities
{
    [Table("CTR_CUENTA_EMAIL")]
    public class CuentaEmailEntity
    {
        [Key]
        [Column("ID_CUENTA")]
        public long IdCuenta { get; set; }

        [Column("ID_DOMINIO_GRUPO")]
        public int? IdDominioGrupo { get; set; }

        [Column("NOMBRE_CUENTA")]
        public string? NombreCuenta { get; set; }

        [Column("EMAIL")]
        public string? Email { get; set; }

        [Column("SERVIDOR_SMTP")]
        public string? ServidorSmtp { get; set; }

        [Column("PUERTO")]
        public int Puerto { get; set; } = 587;

        [Column("USUARIO_SMTP")]
        public string? UsuarioSmtp { get; set; }

        [Column("CLAVE_SMTP")]
        public string? ClaveSmtp { get; set; }

        [Column("USAR_SSL")]
        public int UsarSsl { get; set; } = 1;

        [Column("VIGENTE")]
        public int Vigente { get; set; } = 1;

        [Column("FIRMA_HTML")]
        public string? FirmaHtml { get; set; }

        [Column("IMAGEN_ENCABEZADO")]
        public string? ImagenEncabezado { get; set; }

        [Column("PRIORIDAD_ALTA")]
        public int PrioridadAlta { get; set; } = 0;

        [Column("ACUSE_RECIBIDO")]
        public int AcuseRecibido { get; set; } = 0;

        [Column("COPIA_PRIVADA")]
        public int CopiaPrivada { get; set; } = 0;

        [Column("USUARIO_CREA")]
        public string? UsuarioCrea { get; set; }

        [Column("FECHA_CREA")]
        public DateTime? FechaCrea { get; set; } = DateTime.Now;

        [Column("USUARIO_MODIFICA")]
        public string? UsuarioModifica { get; set; }

        [Column("FECHA_MODIFICA")]
        public DateTime? FechaModifica { get; set; }

        [Column("IP_REGISTRO")]
        public string? IpRegistro { get; set; }
    }

    [Table("CTR_CUENTA_EMAIL_USUARIO")]
    public class CuentaEmailUsuarioEntity
    {
        [Key]
        [Column("ID_CUENTA_USUARIO")]
        public long IdCuentaUsuario { get; set; }

        [Column("ID_CUENTA")]
        public long IdCuenta { get; set; }

        [Column("ID_USUARIO")]
        public long IdUsuario { get; set; }

        [Column("VIGENTE")]
        public int Vigente { get; set; } = 1;

        [Column("USUARIO_CREA")]
        public string? UsuarioCrea { get; set; }

        [Column("FECHA_CREA")]
        public DateTime? FechaCrea { get; set; } = DateTime.Now;

        [Column("USUARIO_MODIFICA")]
        public string? UsuarioModifica { get; set; }

        [Column("FECHA_MODIFICA")]
        public DateTime? FechaModifica { get; set; }

        [Column("IP_REGISTRO")]
        public string? IpRegistro { get; set; }
    }

    [Table("CTR_USUARIOS")]
    public class UsuarioCuentaEntity
    {
        [Key]
        [Column("ID_USUARIO")]
        public long IdUsuario { get; set; }

        [Column("USERNAME")]
        public string? Username { get; set; }

        [Column("NOMBRES")]
        public string? Nombres { get; set; }

        [Column("APELLIDOS")]
        public string? Apellidos { get; set; }

        [Column("IDENTIFICACION")]
        public long? Identificacion { get; set; }
    }

    [Table("CTR_DOMINIOS")]
    public class DominioCuentaEntity
    {
        [Key]
        [Column("ID_DOMINIO")]
        public int IdDominio { get; set; }

        [Column("DESCRIPCION")]
        public string? Descripcion { get; set; }

        [Column("ABREVIATURA")]
        public string? Abreviatura { get; set; }
    }

    [Table("CTR_BRANDING")]
    public class BrandingCuentaEntity
    {
        [Key]
        [Column("ID_BRANDING")]
        public long IdBranding { get; set; }

        [Column("SISTEMA")]
        public string? Sistema { get; set; }

        [Column("NOMBRE_SISTEMA")]
        public string? NombreSistema { get; set; }

        [Column("SIGLA")]
        public string? Sigla { get; set; }

        [Column("VIGENTE")]
        public int Vigente { get; set; } = 1;
    }

    [Table("CTR_ENVIO_CORREO")]
    public class EnvioCorreoEntity
    {
        [Key]
        [Column("ID_ENVIO")]
        public long IdEnvio { get; set; }

        [Column("ID_CUENTA_EMAIL")]
        public long? IdCuentaEmail { get; set; }

        [Column("RADICADO")]
        public string? Radicado { get; set; }

        [Column("FECHA_CREA")]
        public DateTime? FechaCrea { get; set; }
    }
}
