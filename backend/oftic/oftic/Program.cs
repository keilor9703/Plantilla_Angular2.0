using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using Datos.Gestion;
using Datos.Interfaz;
using Negocio.Gestion;
using Negocio.Interfaz;
using Servicios.Api;
using Servicios.ApiInterfaz;
using Negocio.Gestion.GestionDocumental;
using Negocio.Gestion.Administracion;
using Datos.Interfaz.GestionDocumental;
using Datos.Gestion.GestionDocumental;
using Microsoft.EntityFrameworkCore;
using Comun.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Kestrel: aumentar límite de tamaño de request para videos (150MB) y permitir I/O síncrono
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150MB
    options.AllowSynchronousIO = true;
});

builder.Services.AddControllers(options =>
    {
        // En .NET 8 con <Nullable>enable>, los string no-nullables reciben
        // implícitamente [Required(AllowEmptyStrings=false)], lo que rechaza
        // campos vacíos en form-data antes de llegar al controlador.
        // Lo suprimimos porque cada servicio valida sus propias reglas de negocio.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Personalizar la respuesta automática de validación del modelo
// para que loguee los errores y devuelva nuestro formato estándar
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray());

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ModelValidation");
        logger.LogWarning("Validación de modelo fallida en {Action}: {@Errors}",
            context.ActionDescriptor.DisplayName, errors);

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { success = false, message = "Datos del formulario inválidos", errors });
    };
});

// CORS - Políticas separadas para desarrollo y producción
builder.Services.AddCors(options =>
{
    // Política Dev: solo localhost
    options.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());

    // Política Pública: permite cualquier origen para endpoints públicos
    options.AddPolicy("PublicCors", p =>
        p.SetIsOriginAllowed(_ => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var key = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
                {
                    key = "CAMBIAR_EN_PRODUCCION_ClaveSeguraMinimo32Caracteres_2026";
                }
                return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key.Trim())) };
            }
        };
    });

builder.Services.AddAuthorization();

// Rate limiting — protege endpoints anónimos de fuerza bruta
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login y Refresh: máx 10 intentos por minuto por IP
    options.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Request.Headers["X-Forwarded-For"]
                     .FirstOrDefault()?.Split(',')[0].Trim()
                 ?? httpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit      = 10,
            Window           = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit       = 0
        });
    });
});

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("AuthClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddScoped<ITokenProvider, TokenProvider>();

builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddHttpClient<IApiWebOud, ApiWebOud>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<IApiWebToken, ApiWebToken>();

// Cliente del servicio centralizado de doble autenticación (Api2FA) para el 2FA del panel CSIRT
builder.Services.AddHttpClient<IApiMfaClient, ApiMfaClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddScoped<IDbMenuService, DbMenuService>();
builder.Services.AddScoped<IDbMenuRepository, DbMenuRepository>();
builder.Services.AddScoped<IDbRolRepository, DbRolRepository>();
builder.Services.AddScoped<IDbRolService, DbRolService>();
builder.Services.AddScoped<IDbUsuarioRepository, DbUsuarioRepository>();
builder.Services.AddScoped<IDbHomeRepository, DbHomeRepository>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        if (document != null)
        {
            document.Info ??= new OpenApiInfo();
            document.Info.Title = "OFTIC / DOCS API";
            document.Info.Version = "v1";

            document.Components ??= new OpenApiComponents();
            if (document.Components.SecuritySchemes == null)
            {
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
            }

            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Ingrese su Token JWT de autenticación"
            };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<OfticDbContext>(options =>
{
    var cs = builder.Configuration.GetDecryptedConnectionString();
    var p = (builder.Configuration["DatabaseProvider"] ?? "Oracle").Trim();

    if (p.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || p.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseMySql(cs, ServerVersion.AutoDetect(cs));
    }
    else if (p.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || p.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(cs);
    }
    else
    {
        options.UseOracle(cs);
    }
});

builder.Services.AddScoped<IDbAuthRepository, DbAuthRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();


builder.Services.AddScoped<IDbLineaMandoService, DbLineaMandoService>();
builder.Services.AddScoped<IDbLineaMandoRepository, DbLineaMandoRepository>();

builder.Services.AddScoped<IDbPersonajeMesService, DbPersonajeMesService>();
builder.Services.AddScoped<IDbPersonajeMesRepository, DbPersonajeMesRepository>();

builder.Services.AddScoped<IDbDominioService, DbDominioService>();
builder.Services.AddScoped<IDbDominioRepository, DbDominioRepository>();

builder.Services.AddScoped<IDbVideoService, DbVideoService>();
builder.Services.AddScoped<IDbVideoRepository, DbVideoRepository>();

builder.Services.AddScoped<IDbAuditoriaService, DbAuditoriaService>();
builder.Services.AddScoped<IDbAuditoriaRepository, DbAuditoriaRepository>();

builder.Services.AddScoped<IDbUbicacionService, DbUbicacionService>();
builder.Services.AddScoped<IDbUbicacionRepository, DbUbicacionRepository>();
builder.Services.AddScoped<IDbCuentaEmailRepository, DbCuentaEmailRepository>();
builder.Services.AddScoped<IDbGestionCorreosRepository, DbGestionCorreosRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDbBrandingService, DbBrandingService>();
builder.Services.AddScoped<IDbBrandingRepository, DbBrandingRepository>();

// --- SI2FA: Panel administrativo del servicio de Doble Factor de Autenticación ---
builder.Services.AddScoped<Datos.Interfaz.Mfa.IMfaAdminRepository, Datos.Gestion.Mfa.MfaAdminRepository>();
builder.Services.AddScoped<Negocio.Interfaz.Mfa.IMfaAdminService, Negocio.Gestion.Mfa.MfaAdminService>();

var app = builder.Build();

var configuredUploadsBase = app.Configuration["AppSettings:UploadsPath"];
var uploadsBase = string.IsNullOrWhiteSpace(configuredUploadsBase)
    ? Path.Combine(app.Environment.ContentRootPath, "uploads")
    : (Path.IsPathRooted(configuredUploadsBase)
        ? configuredUploadsBase
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, configuredUploadsBase)));

var emailHeadersRoot = Path.Combine(uploadsBase, "email-headers");
Directory.CreateDirectory(emailHeadersRoot);

app.UseCors("DevCors");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("DOCS / OFTIC API - .NET 10")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
    });

    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(emailHeadersRoot),
    RequestPath = "/uploads/email-headers"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

try
{
    await Datos.Gestion.DatabaseMigration.RunAsync(app.Configuration, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error ejecutando la migración inicial de la base de datos");
}

app.Run();
