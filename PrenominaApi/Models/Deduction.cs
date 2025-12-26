using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("deducciones")]
    public class Deduction
    {
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Key]
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Key]
        [Column("folio")]
        public int Folio { get; set; }
        [Key]
        [Column("num_conc")]
        public int NumConc {  get; set; }
        [Key]
        [Column("centro")]
        public required string Centro { get; set; }
        [Key]
        [Column("clase")]
        public required string Clase { get; set; }
        [Key]
        [Column("mes_operacion")]
        public int MonthOperation { get; set; }
        [Key]
        [Column("ayo_operacion")]
        public int YearOperation { get; set; }
        [Key]
        [Column("tiponom")]
        public int TypeNom { get; set; }
        [Key]
        [Column("periodo")]
        public int Period { get; set; }
        [Column("fchinicio")]
        public DateTime? StartDate { get; set; }
        [Column("dias")]
        public decimal Days { get; set; }
    }
}
