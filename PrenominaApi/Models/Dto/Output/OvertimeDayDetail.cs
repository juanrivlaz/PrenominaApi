namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Detalle de tiempo extra por día
    /// </summary>
    public class OvertimeDayDetail
    {
        public DateOnly Date { get; set; }
        public TimeOnly CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }

        /// <summary>
        /// Total de minutos trabajados
        /// </summary>
        public int TotalMinutesWorked { get; set; }

        /// <summary>
        /// Minutos de tiempo extra (sobre 8 hrs)
        /// </summary>
        public int OvertimeMinutes { get; set; }
        public string OvertimeFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Estado del procesamiento
        /// </summary>
        public OvertimeDayStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        /// <summary>
        /// ID del movimiento si ya fue procesado
        /// </summary>
        public int? MovementId { get; set; }
    }
}
