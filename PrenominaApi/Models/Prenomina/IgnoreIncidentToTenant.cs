using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("ignore_incident_to_tenant")]
    public class IgnoreIncidentToTenant
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("incident_code")]
        [Required]
        public required string IncidentCode { get; set; }
        [Column("department_code")]
        public string? DepartmentCode { get; set; }
        [Column("supervisor_id")]
        public int? SupervisorId { get; set; }
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
