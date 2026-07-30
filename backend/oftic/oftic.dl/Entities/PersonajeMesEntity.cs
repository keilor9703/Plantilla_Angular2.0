using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Datos.Entities
{
    [Table("CTR_PERSONAJE_GRUPO")]
    public class PersonajeGrupoEntity
    {
        [Key]
        [Column("ID_PERSONAJE_GRUPO")]
        public long IdPersonajeGrupo { get; set; }

        [Column("NOMBRE_GRUPO")]
        public string? NombreGrupo { get; set; }

        [Column("NUMERO_ACTA")]
        public string? NumeroActa { get; set; }

        [Column("MES")]
        public int? Mes { get; set; }

        [Column("ANIO")]
        public int? Anio { get; set; }

        [Column("VIGENTE")]
        public int Vigente { get; set; } = 1;

        [Column("FOTO_GRUPO")]
        public string? FotoGrupo { get; set; }

        [Column("USUARIO_CREA")]
        public string? UsuarioCrea { get; set; }

        [Column("FECHA_CREA")]
        public DateTime? FechaCrea { get; set; } = DateTime.Now;

        [Column("MAQUINA_CREA")]
        public string? MaquinaCrea { get; set; }

        [Column("USUARIO_MOD")]
        public string? UsuarioMod { get; set; }

        [Column("FECHA_MOD")]
        public DateTime? FechaMod { get; set; }

        [Column("MAQUINA_MOD")]
        public string? MaquinaMod { get; set; }
    }

    [Table("CTR_PERSONAJE_MES")]
    public class PersonajeMesEntity
    {
        [Key]
        [Column("ID_PERSONAJE_MES")]
        public long IdPersonajeMes { get; set; }

        [Column("IDENTIFICACION")]
        public string? Identificacion { get; set; }

        [Column("NOMBRES")]
        public string? Nombres { get; set; }

        [Column("APELLIDOS")]
        public string? Apellidos { get; set; }

        [Column("GRADO")]
        public string? Grado { get; set; }

        [Column("CARGO")]
        public string? Cargo { get; set; }

        [Column("UNIDAD")]
        public string? Unidad { get; set; }

        [Column("ID_CATEGORIA")]
        public int? IdCategoria { get; set; }

        [Column("NUMERO_ACTA")]
        public string? NumeroActa { get; set; }

        [Column("VIGENTE")]
        public int Vigente { get; set; } = 1;

        [Column("FOTO_MODIFICADA")]
        public string? FotoModificada { get; set; }

        [Column("MES")]
        public int? Mes { get; set; }

        [Column("ANIO")]
        public int? Anio { get; set; }

        [Column("ID_PERSONAJE_GRUPO")]
        public long? IdPersonajeGrupo { get; set; }

        [Column("USUARIO_CREA")]
        public string? UsuarioCrea { get; set; }

        [Column("FECHA_CREA")]
        public DateTime? FechaCrea { get; set; } = DateTime.Now;

        [Column("MAQUINA_CREA")]
        public string? MaquinaCrea { get; set; }

        [Column("USUARIO_MOD")]
        public string? UsuarioMod { get; set; }

        [Column("FECHA_MOD")]
        public DateTime? FechaMod { get; set; }

        [Column("MAQUINA_MOD")]
        public string? MaquinaMod { get; set; }
    }
}
