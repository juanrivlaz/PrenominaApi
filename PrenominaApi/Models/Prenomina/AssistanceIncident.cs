using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(CompanyId))]
    [Index(nameof(EmployeeCode))]
    [Index(nameof(Date))]
    [Table("assistance_incident")]
    public class AssistanceIncident
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("company_id")]
        public int CompanyId { get; set; }
        [Column("employee_code")]
        public int EmployeeCode { get; set; }
        [Column("date")]
        public DateOnly Date {  get; set; }
        [Column("incident_code")]
        public required string IncidentCode { get; set; }
        [Column("time_off_request")]
        public bool TimeOffRequest { get; set; }
        [Column("approved")]
        public bool Approved { get; set; }
        [Column("by_user_id")]
        public required Guid ByUserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual IncidentCode? ItemIncidentCode { get; set; }
        [NotMapped]
        public virtual User? User {  get; set; }
        [NotMapped]
        public virtual IEnumerable<AssistanceIncidentApprover>? AssistanceIncidentApprover { get; set; }
    }
}
