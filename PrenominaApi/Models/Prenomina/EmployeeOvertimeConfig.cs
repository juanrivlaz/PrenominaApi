using PrenominaApi.Attributes;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Auditable("Exclusión de horas extras por empleado", SectionCode.OvertimeConfig, IdentifierProperties = new[] { "EmployeeCode" })]
    [Table("employee_overtime_configs")]
    public class EmployeeOvertimeConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("employee_code")]
        public int EmployeeCode { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("exclude_overtime")]
        public bool ExcludeOvertime { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
