using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(EmployeeCode))]
    [Index(nameof(Date))]
    [Table("employee_check_ins")]
    public class EmployeeCheckIns
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("employee_code")]
        public required int EmployeeCode { get; set; }
        [Column("company_id")]
        public decimal CompanyId {  get; set; }
        [Column("check_in")]
        public TimeOnly CheckIn { get; set; }
        [Column("date")]
        public DateOnly Date { get; set; }
        [Column("num_conc")]
        public string? NumConc {  get; set; }
        [Column("EoS")]
        public EntryOrExit EoS { get; set; }
        [Column("period")]
        public int Period { get; set; }
        [Column("type_nom")]
        public int TypeNom {  get; set; }
        [Column("employee_schedule")]
        public int EmployeeSchedule { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
