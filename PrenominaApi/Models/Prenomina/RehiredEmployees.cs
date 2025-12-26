using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(EmployeeCode))]
    [Index(nameof(CompanyId))]
    [Table("rehired_employees")]
    public class RehiredEmployees
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("employee_code")]
        public required int EmployeeCode { get; set; }
        [Column("company_id")]
        public decimal CompanyId { get; set; }
        [Column("contract_folio")]
        public int ContractFolio { get; set; }
        [Column("apply_rehired")]
        public bool ApplyRehired { get; set; }
        [Column("observation")]
        [MaxLength(1500)]
        public string? Observation { get; set; }
        [Column("contract_days")]
        public int ContractDays { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
