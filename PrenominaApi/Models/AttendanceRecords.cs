using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("relch_registro")]
    public class AttendanceRecords
    {
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Column("checada")]
        public string? CheckInOut { get; set; }
        [Column("tiponom")]
        public int? TypeNom {  get; set; }
        [Column("EoS")]
        public string? TypeInOut { get; set; }
        [Column("fecha")]
        public DateOnly Date {  get; set; }

    }
}
