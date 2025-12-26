using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("Nominas")]
    [PrimaryKey("Company", "TypeNom")]
    public class Payroll
    {
        [Column("empresa")]
        public decimal Company {  get; set; }
        [Column("tiponom")]
        public int TypeNom {  get; set; }
        [Column("nomina")]
        public string? Label {  get; set; }
        [Column("frecuencia")]
        public required int Days { get; set; }
    }
}
