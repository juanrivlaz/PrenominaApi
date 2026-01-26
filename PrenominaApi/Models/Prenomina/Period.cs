using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("period")]
    public class Period
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
        public required decimal Company { get; set; }
        [Column("start_date")]
        public required DateOnly StartDate { get; set; }
        [Column("closing_date")]
        public required DateOnly ClosingDate { get; set; }
        [Column("date_payment")]
        public required DateOnly DatePayment { get; set; }
        [Column("total_days")]
        public required int TotalDays { get; set; }
        [Column("start_admin_date")]
        public required DateOnly StartAdminDate { get; set; }
        [Column("closing_admin_date")]
        public required DateOnly ClosingAdminDate { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = false;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
