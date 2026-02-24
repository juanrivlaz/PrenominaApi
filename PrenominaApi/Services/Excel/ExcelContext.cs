using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Reports;

namespace PrenominaApi.Services.Excel
{
    public class ExcelContext
    {
        public IEnumerable<ReportOvertimesOutput>? reportOvertimes { get; set; }
        public IEnumerable<ReportDelaysOutput>? reportDelays { get; set; }
        public IEnumerable<ReportHoursWorkedOutput>? reportHoursWorkeds { get; set; }
        public IEnumerable<ReportAttendanceOutput>? reportAttendances { get; set; }
        public IEnumerable<ReportIncidencesOutput>? reportIncidence { get; set; }
    }
}
