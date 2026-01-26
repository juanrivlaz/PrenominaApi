using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("incident_code_allowed_roles")]
    public class IncidentCodeAllowedRoles
    {
        [Column("incident_code")]
        public required string IncidentCode { get; set; }
        [Column("role_code")]
        public required Guid RoleId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual IncidentCode? ItemIncidentCode { get; set; }
    }
}
