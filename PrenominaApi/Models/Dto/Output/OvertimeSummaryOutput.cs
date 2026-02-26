namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Resumen de horas extras para el reporte
    /// </summary>
    public class OvertimeSummaryOutput
    {
        public int EmployeeCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobPosition { get; set; } = string.Empty;

        /// <summary>
        /// Total de minutos de tiempo extra en el período
        /// </summary>
        public int TotalOvertimeMinutes { get; set; }
        public string TotalOvertimeFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Minutos ya acumulados del período
        /// </summary>
        public int AccumulatedMinutes { get; set; }

        /// <summary>
        /// Minutos ya pagados del período
        /// </summary>
        public int PaidMinutes { get; set; }

        /// <summary>
        /// Minutos pendientes por procesar
        /// </summary>
        public int PendingMinutes { get; set; }

        /// <summary>
        /// Balance actual disponible
        /// </summary>
        public int CurrentBalance { get; set; }
        public string CurrentBalanceFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Detalle por día
        /// </summary>
        public List<OvertimeDayDetail> DayDetails { get; set; } = new();
    }
}
