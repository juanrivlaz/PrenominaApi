using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("assistance_incident_approver")]
    public class AssistanceIncidentApprover
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("assistance_incident_id")]
        public required Guid AssistanceIncidentId { get; set; }
        [Column("incident_approver_id")]
        public required Guid IncidentApproverId { get; set; }
        [Column("approval_date")]
        public DateTime ApprovalDate { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        public required AssistanceIncident AssistanceIncident { get; set; }
        public required IncidentApprover IncidentApprover { get; set; }
    }
}
