using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("period_status")]
    public class PeriodStatus
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("type_payroll")]
        public int TypePayroll { get; set; }
        [Column("num_period")]
        public required int NumPeriod { get; set; }
        [Column("year")]
        public required int Year { get; set; }
        [Column("company")]
        public required decimal CompanyId { get; set; }
        [Column("tenant_id")]
        public required string TenantId { get; set; }
        [Column("by_user_id")]
        public Guid ByUserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
