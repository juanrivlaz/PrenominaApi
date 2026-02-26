using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Log de movimientos de horas extras para trazabilidad
    /// </summary>
    [Table("overtime_movement_logs")]
    public class OvertimeMovementLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("overtime_accumulation_id")]
        public int OvertimeAccumulationId { get; set; }

        [Required]
        [Column("employee_code")]
        public int EmployeeCode { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        /// <summary>
        /// Tipo de movimiento
        /// </summary>
        [Required]
        [Column("movement_type")]
        public OvertimeMovementType MovementType { get; set; }

        /// <summary>
        /// Minutos involucrados en el movimiento (positivo o negativo)
        /// </summary>
        [Required]
        [Column("minutes")]
        public int Minutes { get; set; }

        /// <summary>
        /// Balance de minutos después de este movimiento
        /// </summary>
        [Required]
        [Column("balance_after")]
        public int BalanceAfter { get; set; }

        /// <summary>
        /// Fecha origen del tiempo extra (cuándo se trabajó)
        /// </summary>
        [Required]
        [Column("source_date")]
        public DateOnly SourceDate { get; set; }

        /// <summary>
        /// Fecha en que se aplicó el día de descanso (si aplica)
        /// </summary>
        [Column("applied_rest_date")]
        public DateOnly? AppliedRestDate { get; set; }

        /// <summary>
        /// Hora de entrada original (referencia)
        /// </summary>
        [Column("original_check_in")]
        public TimeOnly? OriginalCheckIn { get; set; }

        /// <summary>
        /// Hora de salida original (referencia)
        /// </summary>
        [Column("original_check_out")]
        public TimeOnly? OriginalCheckOut { get; set; }

        /// <summary>
        /// Notas o comentarios del movimiento
        /// </summary>
        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Usuario que registró el movimiento
        /// </summary>
        [Required]
        [Column("by_user_id")]
        public Guid ByUserId { get; set; }

        /// <summary>
        /// Referencia al movimiento original (para cancelaciones)
        /// </summary>
        [Column("related_movement_id")]
        public int? RelatedMovementId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OvertimeAccumulationId")]
        public virtual OvertimeAccumulation? OvertimeAccumulation { get; set; }

        [ForeignKey("ByUserId")]
        public virtual User? User { get; set; }

        [ForeignKey("RelatedMovementId")]
        public virtual OvertimeMovementLog? RelatedMovement { get; set; }
    }
}
