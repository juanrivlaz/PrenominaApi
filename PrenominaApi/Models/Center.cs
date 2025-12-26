using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("centros")]
    public class Center
    {
        [Key]
        [Column("centro")]
        public required string Id { get; set; }
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("nomdepto")]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public IEnumerable<Key>? Keys { get; set; }
    }
}
