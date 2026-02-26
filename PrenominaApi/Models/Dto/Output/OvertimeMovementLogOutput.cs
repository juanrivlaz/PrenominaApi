using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Detalle de un movimiento de horas extras
    /// </summary>
    public class OvertimeMovementLogOutput
    {
        public int Id { get; set; }
        public int EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public OvertimeMovementType MovementType { get; set; }
        public string MovementTypeLabel { get; set; } = string.Empty;

        /// <summary>
        /// Minutos del movimiento (positivo = acumulación, negativo = uso/pago)
        /// </summary>
        public int Minutes { get; set; }

        /// <summary>
        /// Formato legible del tiempo (ej: "2 hrs 15 min")
        /// </summary>
        public string MinutesFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Balance después del movimiento
        /// </summary>
        public int BalanceAfter { get; set; }
        public string BalanceAfterFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de donde proviene el tiempo extra
        /// </summary>
        public DateOnly SourceDate { get; set; }

        /// <summary>
        /// Fecha donde se aplicó el descanso (si aplica)
        /// </summary>
        public DateOnly? AppliedRestDate { get; set; }

        public TimeOnly? OriginalCheckIn { get; set; }
        public TimeOnly? OriginalCheckOut { get; set; }

        public string? Notes { get; set; }
        public string CreatedByUser { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indica si el movimiento fue cancelado
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// ID del movimiento de cancelación (si aplica)
        /// </summary>
        public int? CancellationMovementId { get; set; }
    }
}
