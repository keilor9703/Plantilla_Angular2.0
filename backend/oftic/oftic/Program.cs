using Microsoft.AspNetCore.Authentication.JwtBearer;
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

var builder = WebApplication.CreateBuilder(args);

// Kestrel: aumentar límite de tamaño de request para videos (150MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150MB
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
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<IApiWebToken, ApiWebToken>();

builder.Services.AddScoped<IDbMenuService, DbMenuService>();
builder.Services.AddScoped<IDbMenuRepository, DbMenuRepository>();
builder.Services.AddScoped<IDbUsuarioRepository, DbUsuarioRepository>();
builder.Services.AddScoped<IDbHomeRepository, DbHomeRepository>();
builder.Services.AddOpenApi();

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
            .WithTitle("SISGE / OFTIC API - .NET 10")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
    });
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
