using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("activity_overtime_configs")]
    public class ActivityOvertimeConfig
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
        [Column("exclude_overtime")]
        public bool ExcludeOvertime { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
