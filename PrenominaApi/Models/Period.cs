using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("Periodos")]
    [PrimaryKey("TypeNom", "Number", "YearOfOperation", "Company")]
    public class Period
    {
        [Column("tiponom")]
        public int TypeNom {  get; set; }
        [Column("periodo")]
        public int Number {  get; set; }
        [Column("ayo_operacion")]
        public int YearOfOperation { get; set; }
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("inicio")]
        public DateTime? StartDate { get; set; }
        [Column("cierre")]
        public DateTime? EndDate { get; set; }
        [Column("pago")]
        public DateTime? PayDate { get; set; }
        [Column("dias")]
        public decimal? Days {  get; set; }
        [Column("mes")]
        public string? Month { get; set; }
        [Column("fchadmin1")]
        public DateTime? StartDateAdmin { get; set; }
        [Column("fchadmin2")]
        public DateTime? EndDateAdmin { get; set; }
    }
}
