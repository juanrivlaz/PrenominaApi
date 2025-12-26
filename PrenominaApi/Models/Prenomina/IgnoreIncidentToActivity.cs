using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("ignore_incident_to_activity")]
    public class IgnoreIncidentToActivity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("incident_code")]
        [Required]
        public required string IncidentCode { get; set; }
        [Column("activity_id")]
        [Required]
        public int ActivityId { get; set; }
        [Column("ignore")]
        [Required]
        public bool Ignore { get; set; } = false;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public IncidentCode? IncidentCodeItem { get; set; }
    }
}
