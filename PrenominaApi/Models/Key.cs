using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("Llaves")]
    public class Key
    {
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Column("centro")]
        public required string Center { get; set; }
        [Column("clase")]
        public required string Clase { get; set; }
        [Column("supervisor")]
        public decimal Supervisor { get; set; }
        [Column("ocupacion")]
        public int Ocupation { get; set; }
        [Column("horario")]
        public int Schedule { get; set; }
        [Column("tiponom")]
        public int TypeNom {  get; set; }
        [Column("banco")]
        public int Bank { get; set; }
        [Column("linea")]
        public decimal Line {  get; set; }

        [NotMapped]
        public virtual IEnumerable<Employee>? Employees { get; set; }
        [NotMapped]
        public virtual required Tabulator Tabulator { get; set; }
        [NotMapped]
        public virtual Center? CenterItem { get; set; }
        public virtual Supervisor? SupervisorItem { get; set; }
        [NotMapped]
        public virtual required Employee Employee { get; set; }
    }
}
