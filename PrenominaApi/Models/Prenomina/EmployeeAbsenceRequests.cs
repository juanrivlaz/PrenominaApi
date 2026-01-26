using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("employee_absence_requests")]
    public class EmployeeAbsenceRequests
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("employee_code")]
        public required int EmployeeCode { get; set; }
        [Column("company_id")]
        public required decimal CompanyId { get; set; }
        [Column("incident_code")]
        public required string IncidentCode { get; set; }
        [Column("start_date")]
        public required DateOnly StartDate { get; set; }
        [Column("end_date")]
        public required DateOnly EndDate { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("status")]
        public AbsenceRequestStatus Status { get; set; }
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
