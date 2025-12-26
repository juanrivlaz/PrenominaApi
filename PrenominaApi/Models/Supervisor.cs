using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("supervisores")]
    public class Supervisor
    {
        [Key]
        [Column("supervisor")]
        public decimal Id { get; set; }
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("nombre")]
        public string? Name { get; set; }
        [Column("turno")]
        public string? Shift { get; set; }
        [Column("codigo")]
        public decimal? Code { get; set; }
        [NotMapped]
        public virtual required IEnumerable<Key> Keys { get; set; }
    }
}
