# API portable: Redes Sociales `Mostrar`

Este documento consolida el flujo mínimo para exponer consultas de redes sociales en otro servicio:
- `GET /api/gestion-digital/redes-sociales` (listar)
- `GET /api/gestion-digital/redes-sociales/{id}` (detalle)

Contrato Oracle usado:
- `PK_APIS.P_GET_REDES_SOCIALES(P_IdRedSocial IN NUMBER, P_Result OUT SYS_REFCURSOR)`

## 1) DTO de salida

```csharp
namespace Comun.Dtos;

public class DtoRedesSociales
{
    public int Id { get; set; }
    public string RedSocial { get; set; } = string.Empty;
    public string UnidadResponsable { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string UrlCuenta { get; set; } = string.Empty;
    public string ActaAsignacion { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string TelResponsable { get; set; } = string.Empty;
    public string EmailResponsable { get; set; } = string.Empty;
    public string EmailCuenta { get; set; } = string.Empty;
    public string TelAsociado { get; set; } = string.Empty;
    public int Vigente { get; set; } = 1;
    public DateTime? FechaCreacion { get; set; }
    public string? UsuarioCreacion { get; set; }
}
```

## 2) Interfaz de repositorio (solo mostrar)

```csharp
using Comun.Dtos;

namespace Datos.Interfaz;

public interface IRedesSocialesPublicRepository
{
    Task<IEnumerable<DtoRedesSociales>> GetAllAsync();
    Task<DtoRedesSociales?> GetByIdAsync(int id);
}
```

## 3) Implementación DB (Oracle ODP.NET)

```csharp
using Comun.Dtos;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion;

public class RedesSocialesPublicRepository : IRedesSocialesPublicRepository
{
    private readonly string _cs;
    private readonly ILogger<RedesSocialesPublicRepository> _logger;

    public RedesSocialesPublicRepository(IConfiguration configuration, ILogger<RedesSocialesPublicRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    public async Task<IEnumerable<DtoRedesSociales>> GetAllAsync()
    {
        var redesSociales = new List<DtoRedesSociales>();

        try
        {
            using var connection = new OracleConnection(_cs);
            await connection.OpenAsync();

            using var command = new OracleCommand("PK_APIS.P_GET_REDES_SOCIALES", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value = DBNull.Value;
            command.Parameters.Add("P_Result", OracleDbType.RefCursor, ParameterDirection.Output);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                redesSociales.Add(MapRedSocial(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las redes sociales");
            throw;
        }

        return redesSociales;
    }

    public async Task<DtoRedesSociales?> GetByIdAsync(int id)
    {
        DtoRedesSociales? redesSociales = null;

        try
        {
            using var connection = new OracleConnection(_cs);
            await connection.OpenAsync();

            using var command = new OracleCommand("PK_APIS.P_GET_REDES_SOCIALES", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value = id;
            command.Parameters.Add("P_Result", OracleDbType.RefCursor, ParameterDirection.Output);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                redesSociales = MapRedSocial(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener red social por ID: {Id}", id);
            throw;
        }

        return redesSociales;
    }

    private static DtoRedesSociales MapRedSocial(OracleDataReader reader)
    {
        return new DtoRedesSociales
        {
            Id = GetInt(reader, "ID_REDES_SOCIALES"),
            RedSocial = GetString(reader, "RED_SOCIAL"),
            UnidadResponsable = GetString(reader, "UNIDAD_RESPONSABLE"),
            NombreCuenta = GetString(reader, "NOMBRE_CUENTA"),
            UrlCuenta = GetString(reader, "URL_CUENTA"),
            ActaAsignacion = GetString(reader, "ACTA_ASIGNACION"),
            NombreResponsable = GetString(reader, "NOMBRE_RESPONSABLE"),
            TelResponsable = GetString(reader, "TELEFONO_RESPONSABLE"),
            EmailResponsable = GetString(reader, "EMAIL_RESPONSABLE"),
            EmailCuenta = GetString(reader, "EMAIL_CUENTA"),
            TelAsociado = GetString(reader, "TELEFONO_ASOCIADO"),
            Vigente = GetInt(reader, "VIGENTE", 1),
            UsuarioCreacion = GetStringOrNull(reader, "USUARIO_CREACION"),
            FechaCreacion = GetDateOrNull(reader, "FECHA_CREACION")
        };
    }

    private static int GetOrdinalSafe(OracleDataReader reader, string column)
    {
        try
        {
            return reader.GetOrdinal(column);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    private static string GetString(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index)) return string.Empty;
        return reader.GetString(index);
    }

    private static string? GetStringOrNull(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index)) return null;
        return reader.GetString(index);
    }

    private static int GetInt(OracleDataReader reader, string column, int defaultValue = 0)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index)) return defaultValue;

        var value = reader.GetValue(index);
        return value switch
        {
            int i => i,
            long l => Convert.ToInt32(l),
            decimal d => Convert.ToInt32(d),
            OracleDecimal od when !od.IsNull => od.ToInt32(),
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : defaultValue
        };
    }

    private static DateTime? GetDateOrNull(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index)) return null;

        var value = reader.GetValue(index);
        return value switch
        {
            DateTime dt => dt,
            OracleDate od when !od.IsNull => od.Value,
            _ => DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null
        };
    }
}
```

## 4) Interfaz de servicio (solo mostrar)

```csharp
using Comun.Dtos;

namespace Negocio.Interfaz;

public interface IRedesSocialesPublicService
{
    Task<IEnumerable<DtoRedesSociales>> GetAllAsync();
    Task<DtoRedesSociales?> GetByIdAsync(int id);
}
```

## 5) Implementación de servicio

```csharp
using Comun.Dtos;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class RedesSocialesPublicService : IRedesSocialesPublicService
{
    private readonly IRedesSocialesPublicRepository _repository;

    public RedesSocialesPublicService(IRedesSocialesPublicRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<DtoRedesSociales>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<DtoRedesSociales?> GetByIdAsync(int id)
        => _repository.GetByIdAsync(id);
}
```

## 6) Controller (`GET` mostrar)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;

namespace Api.Controllers;

[ApiController]
[Route("api/gestion-digital/redes-sociales")]
[Authorize] // Ajustar según política del nuevo servidor
public class RedesSocialesController : ControllerBase
{
    private readonly IRedesSocialesPublicService _service;

    public RedesSocialesController(IRedesSocialesPublicService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var redesSociales = await _service.GetAllAsync();
        return Ok(redesSociales);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var redesSociales = await _service.GetByIdAsync(id);
        if (redesSociales == null)
        {
            return NotFound();
        }

        return Ok(redesSociales);
    }
}
```

## 7) Snippet de `Program.cs` (DI + conexión)

```csharp
using Datos.Gestion;
using Datos.Interfaz;
using Negocio.Gestion;
using Negocio.Interfaz;

builder.Services.AddScoped<IRedesSocialesPublicService, RedesSocialesPublicService>();
builder.Services.AddScoped<IRedesSocialesPublicRepository, RedesSocialesPublicRepository>();
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
  - `P_GET_REDES_SOCIALES(P_IdRedSocial IN NUMBER, P_Result OUT SYS_REFCURSOR)`

## Checklist de copiado al nuevo servidor

1. Copiar DTO + interfaces + repo + service + controller.
2. Registrar DI en `Program.cs`.
3. Configurar cadena `DbCoest`.
4. Verificar permisos `EXECUTE` sobre `PK_APIS.P_GET_REDES_SOCIALES`.
5. Validar seguridad (`[Authorize]` o política que definas en el servidor API especial).

## Plan de pruebas sugerido

1. Compilar proyecto destino.
2. Probar `GET /api/gestion-digital/redes-sociales` y validar HTTP 200 con lista.
3. Probar `GET /api/gestion-digital/redes-sociales/{id}` con id existente (200) y no existente (404).
4. Simular fallo de permisos/BD y validar manejo de error y logging.
