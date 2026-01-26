using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("work_schedule")]
    public class WorkSchedule
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("company")]
        public required decimal Company { get; set; }
        [Column("label")]
        public required string Label { get; set; }
        [Column("start_time")]
        public required TimeOnly StartTime { get; set; }
        [Column("end_time")]
        public required TimeOnly EndTime { get; set; }
        [Column("break_start")]
        public TimeOnly? BreakStart { get; set; }
        [Column("break_end")]
        public TimeOnly? BreakEnd { get; set; }
        [Column("work_hours")]
        public required Decimal WorkHours { get; set; }
        [Column("is_night_shift")]
        public required bool IsNightShift { get; set; } = false;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;

    }
}
