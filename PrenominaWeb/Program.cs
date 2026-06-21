using Microsoft.Extensions.FileProviders;

// Servidor estático mínimo para el frontend Angular (SPA).
// Se ejecuta como Servicio de Windows (Kestrel autónomo, sin IIS ni Node).
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Prenomina Web";
});

// Como servicio el directorio de trabajo es System32; forzar el del ejecutable.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var app = builder.Build();

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
Directory.CreateDirectory(webRoot);
var fileProvider = new PhysicalFileProvider(webRoot);

// index.html como archivo por defecto
var defaultFiles = new DefaultFilesOptions { FileProvider = fileProvider };
defaultFiles.DefaultFileNames.Clear();
defaultFiles.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(defaultFiles);

// Servir estáticos. No cachear index.html ni runtime-config.json para que los
// cambios de configuración (URL del API) apliquen de inmediato.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    OnPrepareResponse = ctx =>
    {
        var name = ctx.File.Name;
        if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("runtime-config.json", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
    }
});

// Fallback SPA: cualquier ruta desconocida devuelve index.html (Angular router).
app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });

app.Run();
