# API pública portable: Radio `GetPublicas`


## 1) DTO de salida

```csharp
namespace Comun.Dtos.Radio
{
    public class DtoRadioEmisora
    {
        public long idEmisora { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string streamUrl { get; set; } = string.Empty;
        public string? logoUrl { get; set; }
        public int orden { get; set; }
        public int activo { get; set; }
    }
}
```

## 2) Interfaz de repositorio público

```csharp
using Comun.Dtos.Radio;

namespace Datos.Interfaz
{
    public interface IRadioPublicRepository
    {
        Task<List<DtoRadioEmisora>> GetPublicasAsync(CancellationToken ct);
    }
}
```

## 3) Implementación DB (Oracle ODP.NET)

```csharp
using Comun.Dtos.Radio;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion
{
    public class RadioPublicRepository : IRadioPublicRepository
    {
        private readonly string _cs;

        public RadioPublicRepository(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DbCoest")!;
        }

        public async Task<List<DtoRadioEmisora>> GetPublicasAsync(CancellationToken ct)
        {
            var result = new List<DtoRadioEmisora>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_APIS.P_GET_ALL_RADIO";
            cmd.Parameters.Add("p_solo_activos", OracleDbType.Int32).Value = 1;
            cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new DtoRadioEmisora
                {
                    idEmisora = GetInt64(reader, "ID_EMISORA"),
                    nombre = GetString(reader, "NOMBRE"),
                    streamUrl = GetString(reader, "STREAM_URL"),
                    logoUrl = reader.IsDBNull(reader.GetOrdinal("LOGO_URL")) ? null : reader.GetString(reader.GetOrdinal("LOGO_URL")),
                    orden = GetInt32(reader, "ORDEN"),
                    activo = GetInt32(reader, "ACTIVO")
                });
            }

            return result;
        }

        private static string GetString(OracleDataReader reader, string column)
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx) ? string.Empty : reader.GetString(idx);
        }

        private static int GetInt32(OracleDataReader reader, string column)
        {
            var idx = reader.GetOrdinal(column);
            if (reader.IsDBNull(idx))
            {
                return 0;
            }

            var value = reader.GetValue(idx);
            if (value is OracleDecimal od)
            {
                return od.IsNull ? 0 : od.ToInt32();
            }

            return Convert.ToInt32(value);
        }

        private static long GetInt64(OracleDataReader reader, string column)
        {
            var idx = reader.GetOrdinal(column);
            if (reader.IsDBNull(idx))
            {
                return 0;
            }

            var value = reader.GetValue(idx);
            if (value is OracleDecimal od)
            {
                return od.IsNull ? 0 : od.ToInt64();
            }

            return Convert.ToInt64(value);
        }
    }
}
```

## 4) Interfaz de servicio público

```csharp
using Comun.Dtos.Radio;

namespace Negocio.Interfaz
{
    public interface IRadioPublicService
    {
        Task<List<DtoRadioEmisora>> GetPublicasAsync(CancellationToken ct);
    }
}
```

## 5) Implementación de servicio

```csharp
using Comun.Dtos.Radio;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion
{
    public class RadioPublicService : IRadioPublicService
    {
        private readonly IRadioPublicRepository _repository;

        public RadioPublicService(IRadioPublicRepository repository)
        {
            _repository = repository;
        }

        public Task<List<DtoRadioEmisora>> GetPublicasAsync(CancellationToken ct)
            => _repository.GetPublicasAsync(ct);
    }
}
```

## 6) Controller público (`GET /api/Radio`)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;

namespace ofic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class RadioController : ControllerBase
    {
        private readonly IRadioPublicService _radioService;
        private readonly ILogger<RadioController> _logger;

        public RadioController(IRadioPublicService radioService, ILogger<RadioController> logger)
        {
            _radioService = radioService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicas()
        {
            try
            {
                var data = await _radioService.GetPublicasAsync(HttpContext.RequestAborted);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando emisoras públicas de radio");
                return StatusCode(500, new { success = false, message = "Error interno consultando emisoras." });
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

builder.Services.AddScoped<IRadioPublicService, RadioPublicService>();
builder.Services.AddScoped<IRadioPublicRepository, RadioPublicRepository>();
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
- Namespaces principales usados:
  - `Comun.Dtos.Radio`
  - `Datos.Interfaz`, `Datos.Gestion`
  - `Negocio.Interfaz`, `Negocio.Gestion`
  - `Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Cors`


