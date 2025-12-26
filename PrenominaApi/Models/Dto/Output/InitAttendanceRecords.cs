using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Output
{
    public class InitAttendanceRecords
    {
        public IEnumerable<Prenomina.Period> Periods { get; set; } = new List<Prenomina.Period>();
        public IEnumerable<Payroll> Payrolls { get; set; } = new List<Payroll>();
        public IEnumerable<IncidentCode> IncidentCodes { get; set; } = new List<IncidentCode>();
        public IEnumerable<PeriodStatus> PeriodStatus { get; set; } = new List<PeriodStatus>();
    }
}
