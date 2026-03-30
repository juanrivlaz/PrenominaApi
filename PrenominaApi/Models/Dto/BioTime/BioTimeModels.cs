using System.Text.Json.Serialization;

namespace PrenominaApi.Models.Dto.BioTime
{
    public class BioTimeAuthRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("company")]
        public string Company { get; set; } = string.Empty;
    }

    public class BioTimeAuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class BioTimePagedResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public List<BioTimeAttendanceRecord> Data { get; set; } = new();
    }

    public class BioTimeAttendanceRecord
    {
        [JsonPropertyName("emp_code")]
        public string EmpCode { get; set; } = string.Empty;

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("att_date")]
        public string AttDate { get; set; } = string.Empty;

        [JsonPropertyName("check_in")]
        public string? CheckIn { get; set; }

        [JsonPropertyName("check_out")]
        public string? CheckOut { get; set; }
    }

    /// <summary>
    /// Configuración almacenada en system_config para la sincronización con BioTime
    /// </summary>
    public class SysBioTimeSyncConfig
    {
        public string SyncHour { get; set; } = "20:00";
        public bool Enabled { get; set; } = false;
    }

    /// <summary>
    /// Credenciales almacenadas de forma cifrada en system_config
    /// </summary>
    public class BioTimeCredentials
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;

        public string GetApiBaseUrl() => $"https://{Company}.biotime.mx";
    }
}
