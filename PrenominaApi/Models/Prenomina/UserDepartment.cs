using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Prenomina
{
    [Table("user_department")]
    public class UserDepartment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("user_company_id")]
        public required Guid UserCompanyId { get; set; }
        [Column("department_code")]
        public required string DepartmentCode { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual UserCompany? UserCompany { get; set; }
    }
}
