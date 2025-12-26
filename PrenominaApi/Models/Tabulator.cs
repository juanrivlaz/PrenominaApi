using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("tabulador")]
    public class Tabulator
    {
        [Key]
        [Column("ocupacion")]
        public int Ocupation {  get; set; }
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("actividad")]
        public string? Activity { get; set; }
        [NotMapped]
        public virtual required IEnumerable<Key> Keys { get; set; }
    }
}
