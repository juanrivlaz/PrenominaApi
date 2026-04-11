using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(EmployeeCode), nameof(CompanyId), nameof(EffectiveFrom))]
    [Index(nameof(WorkScheduleId))]
    [Table("employee_work_schedule_assignment")]
    public class EmployeeWorkScheduleAssignment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("employee_code")]
        public int EmployeeCode { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("work_schedule_id")]
        public Guid WorkScheduleId { get; set; }

        [Required]
        [Column("effective_from")]
        public DateOnly EffectiveFrom { get; set; }

        [Column("effective_to")]
        public DateOnly? EffectiveTo { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [ForeignKey(nameof(WorkScheduleId))]
        public WorkSchedule? WorkSchedule { get; set; }
    }
}
