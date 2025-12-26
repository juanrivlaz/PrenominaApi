using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models
{
    [Table("empresas")]
    public class Company
    {
        [Key]
        [Column("empresa")]
        public decimal Id { get; set; }
        [Column("mascara")]
        public required string Mask { get; set; }
        [Column("mes_operacion")]
        public int MonthOfOperation { get; set; }
        [Column("ayo_operacion")]
        public int YearOfOperation { get; set; }
        [Column("registro_patronal")]
        public required string EmployerRegistration {  get; set; }
        [Column("rfc_empresa")]
        public required string RFC {  get; set; }
        [Column("nombre_empresa")]
        public required string Name { get; set; }
        [Column("direccion_empresa")]
        public required string Address {  get; set; }
    }
}
