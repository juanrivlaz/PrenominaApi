using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("user_company")]
    public class UserCompany
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("user_id")]
        public required Guid UserId { get; set; }
        [Column("company_id")]
        public int CompanyId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual User? User { get; set; }
        [NotMapped]
        public virtual IEnumerable<UserDepartment>? UserDepartments { get; set; }
        [NotMapped]
        public virtual IEnumerable<UserSupervisor>? UserSupervisors { get; set; }
    }
}
