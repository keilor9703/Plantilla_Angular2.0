using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
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
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "oftic.api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? jwtIssuer;

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Configuracion insegura: Jwt:Key no configurada o demasiado corta. Defina una clave de al menos 32 caracteres en appsettings o variables de entorno.");
}

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

builder.Services.AddHttpClient("AuthClient");

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
builder.Services.AddScoped<IDbSliders, DbSliders>();
builder.Services.AddScoped<IDbHomeRepository, DbHomeRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SICOE API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IDbAuthRepository, DbAuthRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();


builder.Services.AddScoped<IDbLineaMandoService, DbLineaMandoService>();
builder.Services.AddScoped<IDbLineaMandoRepository, DbLineaMandoRepository>();

builder.Services.AddScoped<IDbPersonajeMesService, DbPersonajeMesService>();
builder.Services.AddScoped<IDbPersonajeMesRepository, DbPersonajeMesRepository>();

builder.Services.AddScoped<IDbRadioService, DbRadioService>();
builder.Services.AddScoped<IDbRadioRepository, DbRadioRepository>();

builder.Services.AddScoped<IDbDominioService, DbDominioService>();
builder.Services.AddScoped<IDbDominioRepository, DbDominioRepository>();

builder.Services.AddScoped<IDbVideoService, DbVideoService>();
builder.Services.AddScoped<IDbVideoRepository, DbVideoRepository>();

builder.Services.AddScoped<IDbVideoInstitucionalService, DbVideoInstitucionalService>();
builder.Services.AddScoped<IDbVideoInstitucionalRepository, DbVideoInstitucionalRepository>();

builder.Services.AddScoped<IDbNoticiaService, DbNoticiaService>();
builder.Services.AddScoped<IDbNoticiaRepository, DbNoticiaRepository>();

builder.Services.AddScoped<IDbEventoService, DbEventoService>();
builder.Services.AddScoped<IDbEventoRepository, DbEventoRepository>();

builder.Services.AddScoped<IDbAuditoriaService, DbAuditoriaService>();
builder.Services.AddScoped<IDbAuditoriaRepository, DbAuditoriaRepository>();

builder.Services.AddScoped<IDbModalService, DbModalService>();
builder.Services.AddScoped<IDbModalRepository, DbModalRepository>();

builder.Services.AddScoped<IDbRedesSocialesService, DbRedesSocialesService>();
builder.Services.AddScoped<IDbRedesSocialesRepository, DbRedesSocialesRepository>();

builder.Services.AddScoped<IDbUbicacionService, DbUbicacionService>();
builder.Services.AddScoped<IDbUbicacionRepository, DbUbicacionRepository>();
builder.Services.AddScoped<IDbCuentaEmailRepository, DbCuentaEmailRepository>();
builder.Services.AddScoped<IDbGestionCorreosRepository, DbGestionCorreosRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDbBrandingService, DbBrandingService>();
builder.Services.AddScoped<IDbBrandingRepository, DbBrandingRepository>();

// Módulo Recolección de Información (Hechos Noticiosos — USR_COEST)
builder.Services.AddScoped<IDbHechoNoticiosoService, DbHechoNoticiosoService>();
builder.Services.AddScoped<IDbHechoNoticiosoRepository, DbHechoNoticiosoRepository>();

builder.Services.AddScoped<IDbMedioCoestService, DbMedioCoestService>();
builder.Services.AddScoped<IDbMedioCoestRepository, DbMedioCoestRepository>();

builder.Services.AddScoped<IDbIntervinienteCoestService, DbIntervinienteCoestService>();
builder.Services.AddScoped<IDbIntervinienteCoestRepository, DbIntervinienteCoestRepository>();

builder.Services.AddScoped<IDbDominioCoestService, DbDominioCoestService>();
builder.Services.AddScoped<IDbDominioCoestRepository, DbDominioCoestRepository>();

var app = builder.Build();

var configuredUploadsBase = app.Configuration["AppSettings:UploadsPath"];
var uploadsBase = string.IsNullOrWhiteSpace(configuredUploadsBase)
    ? Path.Combine(app.Environment.ContentRootPath, "uploads")
    : (Path.IsPathRooted(configuredUploadsBase)
        ? configuredUploadsBase
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, configuredUploadsBase)));

var uploadsRoot = Path.Combine(uploadsBase, "sliders");
Directory.CreateDirectory(uploadsRoot);

var radioLogosRoot = Path.Combine(uploadsBase, "radio");
Directory.CreateDirectory(radioLogosRoot);

var noticiasRoot = Path.Combine(uploadsBase, "noticias");
Directory.CreateDirectory(noticiasRoot);

var modalesRoot = Path.Combine(uploadsBase, "modales");
Directory.CreateDirectory(modalesRoot);

var eventosRoot = Path.Combine(uploadsBase, "eventos");
Directory.CreateDirectory(eventosRoot);

var emailHeadersRoot = Path.Combine(uploadsBase, "email-headers");
Directory.CreateDirectory(emailHeadersRoot);

app.UseCors("DevCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads/sliders"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(radioLogosRoot),
    RequestPath = "/uploads/radio"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(noticiasRoot),
    RequestPath = "/uploads/noticias"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(eventosRoot),
    RequestPath = "/uploads/eventos"
});

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

app.Run();
