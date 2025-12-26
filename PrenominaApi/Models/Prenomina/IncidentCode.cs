using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(Code), IsUnique = true)]
    [Table("incident_code")]
    public class IncidentCode
    {
        [Key]
        [Column("code")]
        public required string Code { get; set; }
        [Column("external_code")]
        public required string ExternalCode { get; set; }
        [Column("label")]
        public required string Label { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("required_approval")]
        public bool RequiredApproval { get; set; }
        [Column("with_operation")]
        public bool WithOperation { get; set; }
        [Column("is_additional")]
        public bool IsAdditional { get; set; } = false;
        [Column("apply_mode")]
        public IncidentCodeApplyMode ApplyMode { get; set; }
        [Column("metadata_id")]
        public Guid? MetadataId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual IncidentCodeMetadata? IncidentCodeMetadata { get; set; }
        [NotMapped]
        public virtual IEnumerable<AssistanceIncident>? AssistanceIncidents { get; set; }
        [NotMapped]
        public virtual IEnumerable<IncidentApprover>? IncidentApprovers { get; set; }
        [NotMapped]
        public virtual IEnumerable<DayOff>? DayOffs { get; set; }
        [NotMapped]
        public virtual IEnumerable<IgnoreIncidentToEmployee>? IgnoreIncidentToEmployees { get; set; }
        [NotMapped]
        public virtual IEnumerable<IgnoreIncidentToTenant>? IgnoreIncidentToTenants { get; set; }
        [NotMapped]
        public virtual IEnumerable<IgnoreIncidentToActivity>? IgnoreIncidentToActivities { get; set; }
    }
}
