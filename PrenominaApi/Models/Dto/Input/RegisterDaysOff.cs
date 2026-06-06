using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class RegisterDaysOff
    {
        [Required]
        public required IEnumerable<DateOnly> Dates { get; set; }
        [Required]
        public required decimal EmployeeCode { get; set; }
        [Required]
        public required string IncidentCode { get; set; }
        public bool RequireAbsenceRequest { get; set; }
        public decimal CompanyId { get; set; }
        public string? UserId { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Horas extra acumuladas a utilizar por día (opcional). Cada entrada relaciona
        /// una fecha del permiso con los minutos de acumulado que se consumirán ese día.
        /// </summary>
        public List<OvertimeUsageInput>? OvertimeUsages { get; set; }
    }
}
