namespace PrenominaInstaller.Models;

/// <summary>
/// Valores que el usuario captura en el instalador. Es lo único que debe configurar.
/// </summary>
public class InstallConfig
{
    // --- Base de datos (se escriben como variables de entorno de máquina) ---
    public string ServerDb { get; set; } = "";
    public string UserDb { get; set; } = "";
    public string PasswordDb { get; set; } = "";
    public string ApsiDb { get; set; } = "apsisistemas";
    public string PrenominaDb { get; set; } = "PrenominaApi";

    // --- API (se escriben en appsettings.Production.json) ---
    public string JwtKey { get; set; } = "";
    public int Port { get; set; } = 5000;
    public string WebBase { get; set; } = "http://localhost:4200";
    public string TimeZone { get; set; } = "Central Standard Time (Mexico)";

    // --- Despliegue API ---
    public string InstallPath { get; set; } = @"C:\PrenominaApi";
    public string ServiceName { get; set; } = "PrenominaApi";
    public string ServiceDisplayName { get; set; } = "Prenomina API";

    // --- Frontend (Angular, servido por PrenominaWeb) ---
    public bool InstallWeb { get; set; } = true;
    /// <summary>URL del API tal como la verá el navegador del usuario (va a runtime-config.json).</summary>
    public string ApiUrl { get; set; } = "";
    public int WebPort { get; set; } = 80;
    public string WebInstallPath { get; set; } = @"C:\PrenominaWeb";
    public string WebServiceName { get; set; } = "PrenominaWeb";
    public string WebServiceDisplayName { get; set; } = "Prenomina Web";

    /// <summary>Cadena de conexión a la BD principal, usada solo para la prueba de conexión.</summary>
    public string BuildTestConnectionString() =>
        $"Server={ServerDb};Database={ApsiDb};User Id={UserDb};Password={PasswordDb};TrustServerCertificate=True;Connect Timeout=8;";

    /// <summary>Valida los campos obligatorios. Devuelve null si todo está correcto.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerDb)) return "El servidor de base de datos es obligatorio.";
        if (string.IsNullOrWhiteSpace(UserDb)) return "El usuario de base de datos es obligatorio.";
        if (string.IsNullOrWhiteSpace(PasswordDb)) return "La contraseña de base de datos es obligatoria.";
        if (string.IsNullOrWhiteSpace(ApsiDb)) return "El nombre de la base de datos principal (APSI) es obligatorio.";
        if (string.IsNullOrWhiteSpace(PrenominaDb)) return "El nombre de la base de datos de prenómina es obligatorio.";
        if (string.IsNullOrWhiteSpace(JwtKey) || JwtKey.Length < 32)
            return "La clave JWT es obligatoria y debe tener al menos 32 caracteres. Use el botón \"Generar\".";
        if (Port is < 1 or > 65535) return "El puerto debe estar entre 1 y 65535.";
        if (string.IsNullOrWhiteSpace(WebBase)) return "La URL del frontend (CORS) es obligatoria.";
        if (string.IsNullOrWhiteSpace(InstallPath)) return "La ruta de instalación del API es obligatoria.";

        if (InstallWeb)
        {
            if (string.IsNullOrWhiteSpace(ApiUrl)) return "La URL del API (para el frontend) es obligatoria.";
            if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out _)) return "La URL del API no es válida (ej: http://servidor:5000/api).";
            if (WebPort is < 1 or > 65535) return "El puerto del frontend debe estar entre 1 y 65535.";
            if (WebPort == Port) return "El puerto del frontend no puede ser igual al del API.";
            if (string.IsNullOrWhiteSpace(WebInstallPath)) return "La ruta de instalación del frontend es obligatoria.";
        }
        return null;
    }
}
