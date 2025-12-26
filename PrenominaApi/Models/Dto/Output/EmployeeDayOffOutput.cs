using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Output
{
    public class EmployeeDayOffOutput : Employee
    {
        public required List<AssistanceIncident> AttendancesIncident { get; set; }
    }
}
