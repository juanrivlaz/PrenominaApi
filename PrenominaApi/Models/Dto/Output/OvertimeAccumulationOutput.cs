namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Información del balance de acumulación de un empleado
    /// </summary>
    public class OvertimeAccumulationOutput
    {
        public int EmployeeCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobPosition { get; set; } = string.Empty;

        /// <summary>
        /// Minutos disponibles para usar
        /// </summary>
        public int AvailableMinutes { get; set; }

        /// <summary>
        /// Horas y minutos formateados (ej: "12 hrs 30 min")
        /// </summary>
        public string AvailableFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Total histórico de minutos acumulados
        /// </summary>
        public int TotalAccumulatedMinutes { get; set; }

        /// <summary>
        /// Total histórico de minutos usados
        /// </summary>
        public int TotalUsedMinutes { get; set; }

        /// <summary>
        /// Total histórico de minutos pagados
        /// </summary>
        public int TotalPaidMinutes { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
