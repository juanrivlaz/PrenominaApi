using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("kardex")]
    public class Kardex
    {
        [Key]
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Key]
        [Column("nomina")]
        public required string Paysheet { get; set; }
        [Key]
        [Column("centro")]
        public required string Centro { get; set; }
        [Key]
        [Column("num_conc")]
        public int NumConc { get; set; }
        [Key]
        [Column("clase")]
        public required string Class { get; set; }
        [Key]
        [Column("folio")]
        public int Folio { get; set; }
        [Column("fch_cierre")]
        public DateTime? CloseDate { get; set; }
        [Column("fch_inicio")]
        public DateTime? StartDate { get; set; }
        [Column("dias")]
        public decimal Days { get; set; }
    }
}
