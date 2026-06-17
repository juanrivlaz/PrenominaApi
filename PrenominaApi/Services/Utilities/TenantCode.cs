namespace PrenominaApi.Services.Utilities
{
    /// <summary>
    /// Utilidades para comparar códigos de centro/departamento (tenant) de forma robusta.
    /// El centro del empleado (Key.Center) puede venir con ceros a la izquierda ('04')
    /// mientras que el tenant del header llega como int ('4'); por eso la comparación
    /// directa con '==' falla y hay que normalizar ambos lados.
    /// </summary>
    public static class TenantCode
    {
        /// <summary>
        /// Normaliza un código de centro/departamento: si es numérico elimina el padding de
        /// ceros ('04' -> '4'); si no, lo compara sin espacios y en mayúsculas.
        /// </summary>
        public static string Normalize(string? code)
        {
            var trimmed = (code ?? string.Empty).Trim();
            return int.TryParse(trimmed, out var n) ? n.ToString() : trimmed.ToUpperInvariant();
        }
    }
}
