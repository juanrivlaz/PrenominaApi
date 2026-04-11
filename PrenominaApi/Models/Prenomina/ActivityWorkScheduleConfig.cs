using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(ActivityId), nameof(CompanyId), IsUnique = true)]
    [Index(nameof(WorkScheduleId))]
    [Table("activity_work_schedule_configs")]
    public class ActivityWorkScheduleConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("activity_id")]
        public int ActivityId { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("work_schedule_id")]
        public Guid WorkScheduleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(WorkScheduleId))]
        public WorkSchedule? WorkSchedule { get; set; }
    }
}
