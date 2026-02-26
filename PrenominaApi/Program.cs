using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PrenominaApi.Configuration;
using PrenominaApi.Converters;
using PrenominaApi.Data;
using PrenominaApi.Filters;
using PrenominaApi.Helper;
using PrenominaApi.Jobs;
using PrenominaApi.Middlewares;
using PrenominaApi.Models.Dto;
using PrenominaApi.Services.Excel;
using PrenominaApi.Services.Excel.Reports;
using PrenominaApi.Services.Utilities;
using PrenominaApi.Services.Utilities.ContractPdf;
using PrenominaApi.Swagger;
using PrenominaApi.Hubs;
using Serilog;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using PrenominaApi.Services;
using PrenominaApi.Services.Utilities.AdditionalPayPdf;
using PrenominaApi.Services.Utilities.PermissionPdf;
using PrenominaApi.Services.Utilities.AttendancePdf;

var builder = WebApplication.CreateBuilder(args);

// Establecer cultura global a "es-MX"
var cultureInfo = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configuration DBContext - Usar variables de entorno (NUNCA hardcodear credenciales)
var dbServer = Environment.GetEnvironmentVariable("SERVER_DB", EnvironmentVariableTarget.Machine)
    ?? Environment.GetEnvironmentVariable("SERVER_DB");
var dbName = Environment.GetEnvironmentVariable("NAME_APSI_DB", EnvironmentVariableTarget.Machine)
    ?? Environment.GetEnvironmentVariable("NAME_APSI_DB");
var dbNamePrenomina = Environment.GetEnvironmentVariable("NAME_PRENOMINA_DB", EnvironmentVariableTarget.Machine)
    ?? Environment.GetEnvironmentVariable("NAME_PRENOMINA_DB");
var dbUser = Environment.GetEnvironmentVariable("USER_DB", EnvironmentVariableTarget.Machine)
    ?? Environment.GetEnvironmentVariable("USER_DB");
var dbPassword = Environment.GetEnvironmentVariable("PASSWORD_DB", EnvironmentVariableTarget.Machine)
    ?? Environment.GetEnvironmentVariable("PASSWORD_DB");

// Validar que las variables de entorno estén configuradas
if (string.IsNullOrEmpty(dbServer) || string.IsNullOrEmpty(dbName) ||
    string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbPassword))
{
    // En desarrollo, usar appsettings como fallback
    if (builder.Environment.IsDevelopment())
    {
        Log.Warning("Database environment variables not set. Using appsettings.json (development only).");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddDbContext<PrenominaDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("PrenominaConnection")));
    }
    else
    {
        throw new InvalidOperationException(
            "Database environment variables (SERVER_DB, NAME_APSI_DB, NAME_PRENOMINA_DB, USER_DB, PASSWORD_DB) must be set in production.");
    }
}
else
{
    // Construir connection strings con Encrypt=True para producción
    var encryptOption = builder.Environment.IsProduction() ? "Encrypt=True;" : "TrustServerCertificate=True;";
    var connection = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};{encryptOption}";
    var connectionPrenomina = $"Server={dbServer};Database={dbNamePrenomina};User Id={dbUser};Password={dbPassword};{encryptOption}";

    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
        connection,
        sqlOptions => sqlOptions.UseCompatibilityLevel(120)
    ));
    builder.Services.AddDbContext<PrenominaDbContext>(options => options.UseSqlServer(connectionPrenomina));
}

// Config TimeZone
var timeZone = builder.Configuration.GetValue<string>("TimeZone") ?? "Central Standard Time (Mexico)";
builder.Services.AddSingleton<TimeZoneInfo>(TimeZoneInfo.FindSystemTimeZoneById(timeZone));

// Dependency injection
ServicePool.RegistryService(builder);

// Config Base Urls
var configBaseUrl = builder.Configuration.GetRequiredSection(BaseUrlConfiguration.CONFIG_NAME);
builder.Services.Configure<BaseUrlConfiguration>(configBaseUrl);
var baseUrlConfig = configBaseUrl.Get<BaseUrlConfiguration>();

// Config JWT - Obtener clave de variable de entorno
string jwtKey;
try
{
    jwtKey = AuthorizationConstants.GetJwtSecretKey();
}
catch (InvalidOperationException) when (builder.Environment.IsDevelopment())
{
    // Fallback solo en desarrollo
    Log.Warning("JWT_SECRET_KEY not set. Using fallback key (development only).");
#pragma warning disable CS0618 // Type or member is obsolete
    jwtKey = AuthorizationConstants.JWT_SECRET_KEY;
#pragma warning restore CS0618
}

var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(config =>
{
    config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    config.SaveToken = false; // No guardar token en AuthenticationProperties (más seguro)
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1), // Reducir ventana de tiempo
        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Config PasswordHasher
builder.Services.AddSingleton<IPasswordHasher<HasPassword>, CustomPasswordHasher>();

// Config Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Config Global Properties - Cambiar a Scoped para evitar memory leaks
builder.Services.AddScoped<GlobalPropertyService>();

// Inject Utils Services
builder.Services.AddSingleton<PDFService>();
builder.Services.AddSingleton<ContractPdfService>();
builder.Services.AddSingleton<AdditionalPayPdfService>();
builder.Services.AddSingleton<PermissionPdfService>();
builder.Services.AddSingleton<AttendancePdfService>();

// Inject Excel Services
builder.Services.AddScoped<IExcelGenerator, ReportDelaysExcelGenerator>();
builder.Services.AddScoped<IExcelGenerator, ReportHoursWorkedExcelGenerator>();
builder.Services.AddScoped<IExcelGenerator, ReportOvertimeExcelGenerator>();
builder.Services.AddScoped<IExcelGenerator, ReportAttendanceExcelGenerator>();
builder.Services.AddScoped<IExcelGenerator, ReportIncidenceExcelGenerator>();
builder.Services.AddScoped<IExcelGeneratorFactory, ExcelGeneratorFactory>();
builder.Services.AddScoped<ExcelReportService>();

// Register Jobs
builder.Services.AddHostedService<AttendaceJob>();
builder.Services.AddHostedService<ClockJob>();

// Apply Filter Controls
builder.Services.AddScoped<CompanyTenantValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<CompanyTenantFilter>();
});

// Register Socket Notifications
builder.Services.AddSignalR();

// Config Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Rate limit global
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Rate limit específico para login (más restrictivo)
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // Respuesta cuando se excede el límite
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Message = "Demasiadas solicitudes. Por favor, espere un momento antes de intentar nuevamente.",
            RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : 60
        }, cancellationToken);
    };
});

// Config Cors - Más restrictivo
const string CORS_POLICY = "CorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORS_POLICY, corsPolicyBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetValue<string>("baseUrls:webBase") ?? "http://localhost:4200";

        corsPolicyBuilder.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries));

        // Restringir headers permitidos
        corsPolicyBuilder.WithHeaders(
            "Content-Type",
            "Authorization",
            "company",
            "tenant",
            "X-Requested-With",
            "Accept"
        );

        // Restringir métodos HTTP
        corsPolicyBuilder.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH");

        corsPolicyBuilder.AllowCredentials();

        // Headers expuestos al cliente
        corsPolicyBuilder.WithExposedHeaders("Content-Disposition", "X-Total-Count");
    });
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<DateOnlySchemaFilter>();
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Prenomina Api",
        Version = "v1",
    });

    // Configura la seguridad para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
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

    options.CustomSchemaIds(type => type.FullName);
});

// Config Log Service
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/prenomina-api.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var app = builder.Build();

// Implement Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PrenominaDbContext>();

    context.Database.Migrate();
    PrenominaDbContext.Seed(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Usar HTTPS y HSTS en producción
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Agregar headers de seguridad (antes de otros middlewares)
app.UseSecurityHeaders();

// Config Exception Handling
app.UseMiddleware<ExceptionMiddleware>();

// Rate Limiting
app.UseRateLimiter();

app.UseCors(CORS_POLICY);

app.UseAuthentication();

app.UseAuthorization();

// Config Middlewares
app.UseMiddleware<UserMiddleware>();
app.UseMiddleware<SetGlobalPropertyMiddleware>();

app.MapControllers();

// Register Socket Hubs
app.MapHub<NotificationHub>("/socket-notification");

app.Run();
