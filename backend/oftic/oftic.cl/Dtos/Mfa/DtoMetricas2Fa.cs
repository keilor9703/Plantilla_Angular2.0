using System;
using System.Collections.Generic;

namespace Comun.Dtos.Mfa
{
    /// <summary>Métricas agregadas para el tablero del panel CSIRT (SI2FA).</summary>
    public class DtoMetricas2Fa
    {
        // Usuarios
        public long TotalUsuarios { get; set; }
        public long UsuariosHabilitados { get; set; }
        public long UsuariosEnrolados { get; set; }
        public long UsuariosPendientesEnrolar { get; set; }
        public long UsuariosBloqueados { get; set; }

        // Sistemas
        public long SistemasTotales { get; set; }
        public long SistemasActivos { get; set; }

        // Actividad (ventana configurable, por defecto 30 días)
        public int VentanaDias { get; set; }
        public long VerificacionesOk { get; set; }
        public long VerificacionesFallidas { get; set; }
        public long Enrolamientos { get; set; }
        public long Restablecimientos { get; set; }
        public long DispositivosConfiablesActivos { get; set; }

        public List<DtoConteoClave> ActividadPorSistema { get; set; } = new();
        public List<DtoConteoClave> EventosPorTipo { get; set; } = new();
        public List<DtoSerieTiempo> SerieDiaria { get; set; } = new();
    }

    public class DtoConteoClave
    {
        public string Clave { get; set; } = string.Empty;
        public long Total { get; set; }
    }

    public class DtoSerieTiempo
    {
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public long VerificacionesOk { get; set; }
        public long VerificacionesFallidas { get; set; }
        public long Total { get; set; }
    }
}
