using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Representa el balance acumulado de horas extras de un empleado
    /// </summary>
    [Table("overtime_accumulations")]
    public class OvertimeAccumulation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("employee_code")]
        public int EmployeeCode { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        /// <summary>
        /// Total de minutos acumulados disponibles para usar
        /// </summary>
        [Required]
        [Column("accumulated_minutes")]
        public int AccumulatedMinutes { get; set; } = 0;

        /// <summary>
        /// Total de minutos usados históricamente
        /// </summary>
        [Required]
        [Column("used_minutes")]
        public int UsedMinutes { get; set; } = 0;

        /// <summary>
        /// Total de minutos pagados históricamente
        /// </summary>
        [Required]
        [Column("paid_minutes")]
        public int PaidMinutes { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<OvertimeMovementLog> MovementLogs { get; set; } = new List<OvertimeMovementLog>();
    }
}
