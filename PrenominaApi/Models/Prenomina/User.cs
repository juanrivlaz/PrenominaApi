using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(Email), IsUnique = true)]
    [Table("user")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("email")]
        public required string Email { get; set; }
        [Column("password")]
        public required string Password { get; set; }
        [Column("role_id")]
        public required Guid RoleId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt {  get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual Role? Role { get; set; }
        [NotMapped]
        public virtual IEnumerable<AssistanceIncident>? AssistanceIncidents { get; set; }
        [NotMapped]
        public virtual IEnumerable<AuditLog>? AuditLogs { get; set; }
        [NotMapped]
        public virtual IEnumerable<IncidentApprover>? IncidentApprovers { get; set; }
        [NotMapped]
        public virtual List<UserCompany> Companies { get; set; } = new();
    }
}
