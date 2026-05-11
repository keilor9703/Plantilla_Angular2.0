# API portable: Sliders `Mostrar Públicos`

Este documento consolida el flujo mínimo para exponer sliders públicos en otro servicio.

Endpoint objetivo:
- `GET /api/Slider/Publicos`

Contrato Oracle esperado en `PK_APIS`:
- `P_GET_SLIDERS_PUBLICOS(P_CURSOR OUT SYS_REFCURSOR)`

## Validación del backend actual

En el backend actual (ya ajustado), el flujo público de sliders está apuntando a:
- `PK_APIS.P_GET_SLIDERS_PUBLICOS`

El endpoint público ya no usa fallback ni enriquecimiento con `P_GET_SLIDERS_ADMIN`.

## 1) DTO de salida

```csharp
using System;

namespace Comun.Dtos.Sliders
{
    public class DtoSliders
    {
        public long idSlider { get; set; }
        public string? titulo { get; set; }
        public string? subtitulo { get; set; }
        public string? urlImagen { get; set; }
        public string? urlDestino { get; set; }
        public int orden { get; set; }
        public int vigente { get; set; }
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaFin { get; set; }
    }
}
```

## 2) Interfaz de repositorio (solo mostrar)

```csharp
using Comun.Dtos.Sliders;

namespace Datos.Interfaz
{
    public interface ISliderPublicRepository
    {
        Task<List<DtoSliders>> GetPublicosAsync(CancellationToken ct);
    }
}
```

## 3) Implementación DB (Oracle ODP.NET)

```csharp
using Comun.Dtos.Sliders;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion
{
    public class SliderPublicRepository : ISliderPublicRepository
    {
        private readonly string _cs;

        public SliderPublicRepository(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DbCoest")!;
        }

        public async Task<List<DtoSliders>> GetPublicosAsync(CancellationToken ct)
        {
            var result = new List<DtoSliders>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_APIS.P_GET_SLIDERS_PUBLICOS";
            cmd.Parameters.Add("P_CURSOR", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var fieldCount = reader.FieldCount;
                var idxIdSlider = GetOrdinal(reader, fieldCount, 0, "id_slider", "idslider", "id");
                var idxTitulo = GetOrdinal(reader, fieldCount, 1, "titulo", "descripcion");
                var idxSubtitulo = GetOrdinal(reader, fieldCount, 2, "subtitulo", "sub_titulo");
                var idxUrlImagen = GetOrdinal(reader, fieldCount, -1, "url_imagen", "urlimagen", "imagen", "ruta_imagen", "archivo");
                var idxUrlDestino = GetOrdinal(reader, fieldCount, -1, "url_destino", "urldestino", "detalle", "link", "enlace");
                var idxOrden = GetOrdinal(reader, fieldCount, 5, "orden", "posicion");
                var idxVigente = GetOrdinal(reader, fieldCount, 6, "vigente", "estado");
                var idxFechaInicio = GetOrdinal(reader, fieldCount, 7, "fecha_inicio", "fechainicio", "inicio");
                var idxFechaFin = GetOrdinal(reader, fieldCount, 8, "fecha_fin", "fechafin", "fin");

                result.Add(new DtoSliders
                {
                    idSlider = GetInt64OrDefault(reader, fieldCount, idxIdSlider, 0),
                    titulo = GetStringOrNull(reader, fieldCount, idxTitulo),
                    subtitulo = GetStringOrNull(reader, fieldCount, idxSubtitulo),
                    urlImagen = GetStringOrNull(reader, fieldCount, idxUrlImagen),
                    urlDestino = GetStringOrNull(reader, fieldCount, idxUrlDestino),
                    orden = GetInt32OrDefault(reader, fieldCount, idxOrden, 0),
                    vigente = GetInt32OrDefault(reader, fieldCount, idxVigente, 1),
                    fechaInicio = GetDateOrNull(reader, fieldCount, idxFechaInicio),
                    fechaFin = GetDateOrNull(reader, fieldCount, idxFechaFin)
                });
            }

            return result;
        }

        private static int GetOrdinal(OracleDataReader reader, int fieldCount, int fallback, params string[] candidates)
        {
            var normalizedNames = new string[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                normalizedNames[i] = NormalizeColumnName(reader.GetName(i));
            }

            foreach (var c in candidates)
            {
                var candidate = NormalizeColumnName(c);
                for (var i = 0; i < fieldCount; i++)
                {
                    if (normalizedNames[i] == candidate || normalizedNames[i].Contains(candidate, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
            }

            return fallback < fieldCount ? fallback : -1;
        }

        private static string NormalizeColumnName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        }

        private static string? GetStringOrNull(OracleDataReader reader, int fieldCount, int index)
        {
            if (index < 0 || index >= fieldCount || reader.IsDBNull(index)) return null;
            return reader.GetValue(index)?.ToString();
        }

        private static int GetInt32OrDefault(OracleDataReader reader, int fieldCount, int index, int @default)
        {
            if (index < 0 || index >= fieldCount || reader.IsDBNull(index)) return @default;

            var value = reader.GetValue(index);
            if (value is OracleDecimal od) return od.IsNull ? @default : od.ToInt32();
            return int.TryParse(value.ToString(), out var parsed) ? parsed : @default;
        }

        private static long GetInt64OrDefault(OracleDataReader reader, int fieldCount, int index, long @default)
        {
            if (index < 0 || index >= fieldCount || reader.IsDBNull(index)) return @default;

            var value = reader.GetValue(index);
            if (value is OracleDecimal od) return od.IsNull ? @default : od.ToInt64();
            if (long.TryParse(value.ToString(), out var parsed)) return parsed;
            return @default;
        }

        private static DateTime? GetDateOrNull(OracleDataReader reader, int fieldCount, int index)
        {
            if (index < 0 || index >= fieldCount || reader.IsDBNull(index)) return null;

            var value = reader.GetValue(index);
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }
    }
}
```

## 4) Interfaz de servicio (solo mostrar)

```csharp
using Comun.Dtos.Sliders;

namespace Negocio.Interfaz
{
    public interface ISliderPublicService
    {
        Task<List<DtoSliders>> GetPublicosAsync(CancellationToken ct);
    }
}
```

## 5) Implementación de servicio

```csharp
using Comun.Dtos.Sliders;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion
{
    public class SliderPublicService : ISliderPublicService
    {
        private readonly ISliderPublicRepository _repository;

        public SliderPublicService(ISliderPublicRepository repository)
        {
            _repository = repository;
        }

        public Task<List<DtoSliders>> GetPublicosAsync(CancellationToken ct)
            => _repository.GetPublicosAsync(ct);
    }
}
```

## 6) Controller (`GET /api/Slider/Publicos`)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class SliderController : ControllerBase
    {
        private readonly ISliderPublicService _sliderService;
        private readonly ILogger<SliderController> _logger;

        public SliderController(ISliderPublicService sliderService, ILogger<SliderController> logger)
        {
            _sliderService = sliderService;
            _logger = logger;
        }

        [HttpGet("Publicos")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicos()
        {
            try
            {
                var data = await _sliderService.GetPublicosAsync(HttpContext.RequestAborted);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando sliders publicos");
                return StatusCode(500, new { message = "Error al consultar sliders publicos" });
            }
        }
    }
}
```

## 7) Snippet de `Program.cs` (DI + conexión)

```csharp
using Datos.Gestion;
using Datos.Interfaz;
using Negocio.Gestion;
using Negocio.Interfaz;

builder.Services.AddScoped<ISliderPublicService, SliderPublicService>();
builder.Services.AddScoped<ISliderPublicRepository, SliderPublicRepository>();
```

`appsettings.json` (mínimo):

```json
{
  "ConnectionStrings": {
    "DbCoest": "User Id=...;Password=...;Data Source=..."
  }
}
```

## Dependencias requeridas

- NuGet: `Oracle.ManagedDataAccess`
- Configuración: `ConnectionStrings:DbCoest`
- SP en `PK_APIS`:
  - `P_GET_SLIDERS_PUBLICOS(P_CURSOR OUT SYS_REFCURSOR)`

## Checklist de copiado al nuevo servidor

1. Copiar DTO + interfaces + repo + service + controller.
2. Registrar DI en `Program.cs`.
3. Configurar `ConnectionStrings:DbCoest`.
4. Verificar permisos `EXECUTE` sobre `PK_APIS.P_GET_SLIDERS_PUBLICOS`.
5. Validar respuesta de `GET /api/Slider/Publicos`.

## Plan de pruebas sugerido

1. Compilar proyecto destino.
2. Probar `GET /api/Slider/Publicos` y validar HTTP 200.
3. Verificar que el cursor retorna columnas esperadas (`id`, `titulo`, `subtitulo`, `urlImagen`, `urlDestino`, `orden`, `vigente`, fechas).
4. Simular error de permisos/BD y validar manejo de error (500 + log).
