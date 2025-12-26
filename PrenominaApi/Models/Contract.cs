using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("contratos")]
    public class Contract
    {
        [Key]
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Key]
        [Column("folio")]
        public int Folio { get; set; }
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("fchAlta")]
        public DateTime? StartDate { get; set; }
        [Column("dias")]
        public int? Days { get; set; }
        [Column("fchterm")]
        public DateTime? TerminationDate { get; set; }
        [Column("sueldo")]
        public decimal? Salary { get; set; }
    }
}
