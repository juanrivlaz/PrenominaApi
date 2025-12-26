using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(Date), IsUnique = true)]
    [Table("day_off")]
    public class DayOff
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("date")]
        [Required]
        public DateOnly Date { get; set; }
        [Column("incident_code")]
        [Required]
        public required string IncidentCode { get; set; }
        [Column("description")]
        [Required]
        public required string Description { get; set; }
        [Column("is_union")]
        [Required]
        public bool IsUnion { get; set; } = false;
        [Column("is_sunday")]
        public bool IsSunday { get; set; } = false;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual IncidentCode? IncidentCodeItem { get; set; }
    }
}
