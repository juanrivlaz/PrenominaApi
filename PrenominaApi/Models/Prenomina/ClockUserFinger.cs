using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Prenomina
{
    [Table("clock_user_finger")]
    public class ClockUserFinger
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("enroll_number")]
        [Required]
        public required string EnrollNumber { get; set; }
        [Column("finger_index")]
        [Required]
        public int FingerIndex { get; set; }
        [Column("flag")]
        public int Flag { get; set; }
        [Column("finger_base_64")]
        public string? FingerBase64 { get; set; }
        [Column("finger_length")]
        public int FingerLength { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public required ClockUser ClockUser { get; set; }
    }
}
