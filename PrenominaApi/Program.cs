using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PrenominaApi.Configuration;
using PrenominaApi.Converters;
using PrenominaApi.Data;
using PrenominaApi.Helper;
using PrenominaApi.Jobs;
using PrenominaApi.Middlewares;
using PrenominaApi.Models.Dto;
using PrenominaApi.Services.Utilities;
using PrenominaApi.Services.Utilities.ContractPdf;
using PrenominaApi.Swagger;
using Serilog;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Establecer cultura global a "es-MX"
var cultureInfo = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configuration DBContext
var dbServer = Environment.GetEnvironmentVariable("SERVER_DB", EnvironmentVariableTarget.Machine);
var dbName = Environment.GetEnvironmentVariable("NAME_APSI_DB", EnvironmentVariableTarget.Machine);
var dbNamePrenomina = Environment.GetEnvironmentVariable("NAME_PRENOMINA_DB", EnvironmentVariableTarget.Machine);
var dbUser = Environment.GetEnvironmentVariable("USER_DB", EnvironmentVariableTarget.Machine);
var dbPassword = Environment.GetEnvironmentVariable("PASSWORD_DB", EnvironmentVariableTarget.Machine);

var connection = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;";
var connectionPrenomina = $"Server={dbServer};Database={dbNamePrenomina};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;";

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddDbContext<PrenominaDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("PrenominaConnection")));

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddDbContext<PrenominaDbContext>(options => options.UseSqlServer(connectionPrenomina));

// Config TimeZone
var timeZone = builder.Configuration.GetValue<string>("TimeZone") ?? "Central Standard Time (Mexico)";
builder.Services.AddSingleton<TimeZoneInfo>(TimeZoneInfo.FindSystemTimeZoneById(timeZone));

// Dependency injection
ServicePool.RegistryService(builder);

// Config Base Urls 
var configBaseUrl = builder.Configuration.GetRequiredSection(BaseUrlConfiguration.CONFIG_NAME);
builder.Services.Configure<BaseUrlConfiguration>(configBaseUrl);
var baseUrlConfig = configBaseUrl.Get<BaseUrlConfiguration>();

// Config JWT
var key = Encoding.ASCII.GetBytes(AuthorizationConstants.JWT_SECRET_KEY);
builder.Services.AddAuthentication(config =>
{
    config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("Jwt:Key") ?? ""))

    };
});

//Config PasswordHasher
builder.Services.AddSingleton<IPasswordHasher<HasPassword>, CustomPasswordHasher>();

//Config Global Properties
builder.Services.AddSingleton<GlobalPropertyService>();

//Inject Utils Services
builder.Services.AddSingleton<PDFService>();
builder.Services.AddSingleton<ContractPdfService>();

//Register Jobs
builder.Services.AddHostedService<AttendaceJob>();
builder.Services.AddHostedService<ClockJob>();

// Config Cors
const string CORS_POLICY = "CorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORS_POLICY, corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin();
        corsPolicyBuilder.AllowAnyHeader();
        corsPolicyBuilder.AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter()); // proceses format dateonly
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<DateOnlySchemaFilter>(); // custom format dateonly
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
                new string[] {}
            }
        });

    options.CustomSchemaIds(type => type.FullName);
});

//Config Log Service
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

// Config Exception Handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(CORS_POLICY);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();

// Config Middlewares
app.UseMiddleware<UserMiddleware>();
app.UseMiddleware<SetGlobalPropertyMiddleware>();

app.MapControllers();

app.Run();
