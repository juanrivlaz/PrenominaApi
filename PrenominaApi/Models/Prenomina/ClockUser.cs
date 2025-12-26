using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(EnrollNumber), IsUnique = true)]
    [Table("clock_user")]
    public class ClockUser
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("enroll_number")]
        [Required]
        public required string EnrollNumber { get; set; }
        [Column("name")]
        [Required]
        public required string Name { get; set; }
        [Column("privilege")]
        [Required]
        public required int Privilege { get; set; }
        [Column("password")]
        public string? Password { get; set; }
        [Column("enabled")]
        public bool Enabled { get; set; } = true;
        [Column("card_number")]
        public string? CardNumber { get; set; }
        [Column("face_base_64")]
        public string? FaceBase64 { get; set; }
        [Column("face_length")]
        public int FaceLength { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public IEnumerable<ClockUserFinger>? UserFingers { get; set; }
    }
}
