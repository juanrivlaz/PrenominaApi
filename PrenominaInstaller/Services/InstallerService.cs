using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using PrenominaInstaller.Models;

namespace PrenominaInstaller.Services;

/// <summary>
/// Orquesta toda la instalación del API y (opcionalmente) el frontend como
/// Servicios de Windows (Kestrel, sin IIS). Cada paso reporta progreso vía <paramref name="log"/>.
/// </summary>
public class InstallerService
{
    private readonly IProgress<string> _log;

    public InstallerService(IProgress<string> log) => _log = log;

    /// <summary>Carpeta empaquetada con el publish self-contained del API.</summary>
    public static string PayloadPath => Path.Combine(AppContext.BaseDirectory, "api");

    /// <summary>Carpeta empaquetada con el servidor estático del frontend.</summary>
    public static string WebPayloadPath => Path.Combine(AppContext.BaseDirectory, "web");

    /// <summary>Carpeta con el build de Angular (dist/prenomina/browser).</summary>
    public static string WebRootPayloadPath => Path.Combine(AppContext.BaseDirectory, "webroot");

    public static bool PayloadExists =>
        Directory.Exists(PayloadPath) &&
        File.Exists(Path.Combine(PayloadPath, "PrenominaApi.dll"));

    public static bool WebPayloadExists =>
        Directory.Exists(WebPayloadPath) &&
        File.Exists(Path.Combine(WebPayloadPath, "PrenominaWeb.dll")) &&
        Directory.Exists(WebRootPayloadPath) &&
        File.Exists(Path.Combine(WebRootPayloadPath, "index.html"));

    /// <summary>Genera una clave JWT segura de 256 bits en base64.</summary>
    public static string GenerateJwtKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Prueba la conexión a SQL Server con los datos capturados.</summary>
    public static async Task<(bool ok, string message)> TestConnectionAsync(InstallConfig cfg, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(cfg.BuildTestConnectionString());
            await conn.OpenAsync(ct);
            return (true, $"Conexión exitosa a '{cfg.ServerDb}' (BD: {cfg.ApsiDb}).");
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar: {ex.Message}");
        }
    }

    public async Task InstallAsync(InstallConfig cfg, CancellationToken ct)
    {
        Log("=== Iniciando instalación de Prenomina ===");

        if (!PayloadExists)
            throw new InvalidOperationException(
                $"No se encontró el API empaquetado en '{PayloadPath}'. " +
                "Publique el API antes de compilar el instalador (ver README).");

        if (cfg.InstallWeb && !WebPayloadExists)
            throw new InvalidOperationException(
                $"Se solicitó instalar el frontend pero falta el servidor web en '{WebPayloadPath}' " +
                $"o el build de Angular en '{WebRootPayloadPath}'. Ejecute build-installer.ps1 (ver README).");

        await InstallApiAsync(cfg, ct);

        if (cfg.InstallWeb)
            await InstallWebAsync(cfg, ct);

        Log("");
        Log("✅ Instalación completada.");
        Log($"   API:      http://localhost:{cfg.Port}   (Swagger: /swagger)");
        if (cfg.InstallWeb)
            Log($"   Frontend: http://localhost:{cfg.WebPort}");
    }

    // ---------------------------------------------------------------------
    // API
    // ---------------------------------------------------------------------

    private async Task InstallApiAsync(InstallConfig cfg, CancellationToken ct)
    {
        Log("");
        Log("── API ──────────────────────────────");

        await StopAndDeleteServiceIfExists(cfg.ServiceName, ct);
        CopyDirectory(PayloadPath, cfg.InstallPath, ct);
        WriteApiSettings(cfg);
        SetMachineEnvironmentVariables(cfg);

        var exePath = Path.Combine(cfg.InstallPath, "PrenominaApi.exe");
        RequireFile(exePath, "el ejecutable del API");
        CreateWindowsService(cfg.ServiceName, exePath, cfg.ServiceDisplayName,
            "API de Prenomina y asistencia de empleados (Kestrel).");
        ConfigureServiceRecovery(cfg.ServiceName);
        ConfigureFirewall($"Prenomina API {cfg.Port}", cfg.Port);
        StartService(cfg.ServiceName);
        await HealthCheck($"http://localhost:{cfg.Port}/swagger/index.html", ct);
    }

    private void WriteApiSettings(InstallConfig cfg)
    {
        var path = Path.Combine(cfg.InstallPath, "appsettings.Production.json");
        Log($"• Escribiendo configuración del API en '{path}'...");

        var root = new JsonObject
        {
            ["Jwt"] = new JsonObject
            {
                ["Key"] = cfg.JwtKey,
                ["Issuer"] = "prenomina-api.com",
                ["Duration"] = 60
            },
            ["baseUrls"] = new JsonObject { ["webBase"] = cfg.WebBase },
            ["TimeZone"] = cfg.TimeZone,
            ["Kestrel"] = new JsonObject
            {
                ["Endpoints"] = new JsonObject
                {
                    ["Http"] = new JsonObject { ["Url"] = $"http://0.0.0.0:{cfg.Port}" }
                }
            }
        };
        WriteJson(path, root);
    }

    private void SetMachineEnvironmentVariables(InstallConfig cfg)
    {
        Log("• Configurando variables de entorno de máquina...");
        Environment.SetEnvironmentVariable("SERVER_DB", cfg.ServerDb, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("NAME_APSI_DB", cfg.ApsiDb, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("NAME_PRENOMINA_DB", cfg.PrenominaDb, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("USER_DB", cfg.UserDb, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("PASSWORD_DB", cfg.PasswordDb, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", cfg.JwtKey, EnvironmentVariableTarget.Machine);
        Log("  SERVER_DB, NAME_APSI_DB, NAME_PRENOMINA_DB, USER_DB, PASSWORD_DB, JWT_SECRET_KEY");
    }

    // ---------------------------------------------------------------------
    // Frontend
    // ---------------------------------------------------------------------

    private async Task InstallWebAsync(InstallConfig cfg, CancellationToken ct)
    {
        Log("");
        Log("── Frontend ─────────────────────────");

        await StopAndDeleteServiceIfExists(cfg.WebServiceName, ct);

        // Servidor estático
        CopyDirectory(WebPayloadPath, cfg.WebInstallPath, ct);

        // Build de Angular -> wwwroot
        var wwwroot = Path.Combine(cfg.WebInstallPath, "wwwroot");
        if (Directory.Exists(wwwroot)) Directory.Delete(wwwroot, recursive: true);
        CopyDirectory(WebRootPayloadPath, wwwroot, ct);

        // runtime-config.json con la URL del API (configuración en caliente, sin recompilar)
        WriteRuntimeConfig(wwwroot, cfg.ApiUrl);

        // Puerto del servidor estático
        WriteWebSettings(cfg);

        var exePath = Path.Combine(cfg.WebInstallPath, "PrenominaWeb.exe");
        RequireFile(exePath, "el ejecutable del servidor web");
        CreateWindowsService(cfg.WebServiceName, exePath, cfg.WebServiceDisplayName,
            "Servidor del frontend de Prenomina (Kestrel estático).");
        ConfigureServiceRecovery(cfg.WebServiceName);
        ConfigureFirewall($"Prenomina Web {cfg.WebPort}", cfg.WebPort);
        StartService(cfg.WebServiceName);
        await HealthCheck($"http://localhost:{cfg.WebPort}/index.html", ct);
    }

    private void WriteRuntimeConfig(string wwwroot, string apiUrl)
    {
        var path = Path.Combine(wwwroot, "runtime-config.json");
        Log($"• Escribiendo URL del API en '{path}'...");
        WriteJson(path, new JsonObject { ["apiUrl"] = apiUrl });
    }

    private void WriteWebSettings(InstallConfig cfg)
    {
        var path = Path.Combine(cfg.WebInstallPath, "appsettings.Production.json");
        Log($"• Escribiendo configuración del frontend en '{path}'...");
        var root = new JsonObject
        {
            ["Kestrel"] = new JsonObject
            {
                ["Endpoints"] = new JsonObject
                {
                    ["Http"] = new JsonObject { ["Url"] = $"http://0.0.0.0:{cfg.WebPort}" }
                }
            }
        };
        WriteJson(path, root);
    }

    // ---------------------------------------------------------------------
    // Helpers genéricos
    // ---------------------------------------------------------------------

    private async Task StopAndDeleteServiceIfExists(string serviceName, CancellationToken ct)
    {
        if (!ServiceExists(serviceName))
        {
            Log($"• Sin instalación previa de '{serviceName}'.");
            return;
        }

        Log($"• Deteniendo y eliminando servicio existente '{serviceName}'...");
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }
        }
        catch (Exception ex)
        {
            Log($"  (aviso) No se pudo detener limpiamente: {ex.Message}");
        }

        await RunProcessAsync("sc.exe", new[] { "delete", serviceName }, ct, allowFailure: true);
        await Task.Delay(2000, ct); // dar tiempo al SCM a liberar el nombre
    }

    private void CopyDirectory(string sourceRoot, string destRoot, CancellationToken ct)
    {
        Log($"• Copiando archivos a '{destRoot}'...");
        Directory.CreateDirectory(destRoot);

        foreach (var dir in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(dir.Replace(sourceRoot, destRoot));
        }

        var files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            File.Copy(file, file.Replace(sourceRoot, destRoot), overwrite: true);
        }
        Log($"  {files.Length} archivos copiados.");
    }

    private void CreateWindowsService(string serviceName, string exePath, string displayName, string description)
    {
        Log($"• Creando Servicio de Windows '{serviceName}'...");
        // sc create requiere espacio tras cada 'clave='. ArgumentList lo maneja correctamente.
        RunProcess("sc.exe", new[]
        {
            "create", serviceName,
            "binPath=", exePath,
            "start=", "auto",
            "DisplayName=", displayName
        });
        RunProcess("sc.exe", new[] { "description", serviceName, description }, allowFailure: true);
    }

    private void ConfigureServiceRecovery(string serviceName)
    {
        Log("• Configurando reinicio automático ante fallos...");
        RunProcess("sc.exe", new[]
        {
            "failure", serviceName,
            "reset=", "86400",
            "actions=", "restart/5000/restart/5000/restart/5000"
        }, allowFailure: true);
    }

    private void ConfigureFirewall(string ruleName, int port)
    {
        Log($"• Abriendo puerto {port}/TCP en el firewall...");
        RunProcess("netsh.exe", new[]
        {
            "advfirewall", "firewall", "delete", "rule", $"name={ruleName}"
        }, allowFailure: true);
        RunProcess("netsh.exe", new[]
        {
            "advfirewall", "firewall", "add", "rule",
            $"name={ruleName}", "dir=in", "action=allow", "protocol=TCP", $"localport={port}"
        }, allowFailure: true);
    }

    private void StartService(string serviceName)
    {
        Log($"• Iniciando servicio '{serviceName}'...");
        using var sc = new ServiceController(serviceName);
        if (sc.Status != ServiceControllerStatus.Running)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
        }
        Log("  Servicio en ejecución.");
    }

    private async Task HealthCheck(string url, CancellationToken ct)
    {
        Log($"• Verificando {url} ...");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var res = await http.GetAsync(url, ct);
                if (res.IsSuccessStatusCode)
                {
                    Log("  Respondió correctamente.");
                    return;
                }
            }
            catch { /* aún arrancando; reintentar */ }
            await Task.Delay(2000, ct);
        }
        Log("  (aviso) No respondió en el tiempo esperado. Revise los logs en la carpeta 'logs'.");
    }

    // ---------------------------------------------------------------------
    // Utilidades
    // ---------------------------------------------------------------------

    private static void WriteJson(string path, JsonNode node)
    {
        var json = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }

    private static void RequireFile(string path, string what)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontró {what} en '{path}'. Verifique el publish.");
    }

    private static bool ServiceExists(string serviceName) =>
        ServiceController.GetServices().Any(s =>
            string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));

    private void RunProcess(string fileName, string[] args, bool allowFailure = false)
        => RunProcessAsync(fileName, args, CancellationToken.None, allowFailure).GetAwaiter().GetResult();

    private async Task RunProcessAsync(string fileName, string[] args, CancellationToken ct, bool allowFailure = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0 && !allowFailure)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(
                $"'{fileName} {string.Join(' ', args)}' falló (código {proc.ExitCode}). {detail}".Trim());
        }
    }

    private void Log(string message) => _log.Report(message);
}
