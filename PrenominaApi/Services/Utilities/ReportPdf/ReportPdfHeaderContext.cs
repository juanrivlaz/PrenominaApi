namespace PrenominaApi.Services.Utilities.ReportPdf
{
    /// <summary>
    /// Datos del encabezado de los reportes PDF (mismo formato que el reporte de asistencia).
    /// </summary>
    public class ReportPdfHeaderContext
    {
        public string CompanyName { get; set; } = string.Empty;
        public string RfcInfo { get; set; } = string.Empty;
        public string TenantName { get; set; } = "Todos";
        public string TypeNom { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
    }
}
