using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("empleados")]
    public class Employee
    {
        [Key]
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("ap_paterno")]
        public string? LastName { get; set; } = string.Empty;
        [Column("ap_materno")]
        public string? MLastName { get; set; } = string.Empty;
        [Column("nombre")]
        public string? Name { get; set; } = string.Empty;
        [Column("sueldo")]
        public decimal Salary { get; set; }
        [Column("fchantigua")]
        public DateTime? SeniorityDate { get; set; }
        [Column("activo")]
        public char? Active { get; set; }

        [NotMapped]
        public virtual IEnumerable<Key>? Keys { get; set; }
        [NotMapped]
        public virtual Key? Key { get; set; }
    }
}
