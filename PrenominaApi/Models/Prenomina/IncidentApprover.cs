using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(IncidentCode))]
    [Table("incident_approver")]
    public class IncidentApprover
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("incident_code")]
        public required string IncidentCode { get; set; }
        [Column("user_id")]
        public required Guid UserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        public required IncidentCode ItemIncidentCode { get; set; }
        public required User User {  get; set; }
        public IEnumerable<AssistanceIncidentApprover>? AssistanceIncidentApprover { get; set; }
    }
}
