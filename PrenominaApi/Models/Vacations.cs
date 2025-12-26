using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("vacaciones")]
    public class Vacations
    {
        [Key]
        [Column("codigo")]
        public decimal Codigo { get; set; }
        [Key]
        [Column("empresa")]
        public decimal Company { get; set; }
        [Column("fin_disfru")]
        public DateTime FinDisfru { get; set; }
    }
}
