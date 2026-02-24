namespace PrenominaApi.Models.Dto
{
    public class SysConfigReports
    {
        public ConfigDayOffReport ConfigDayOffReport { get; set; }
        public ConfigOvertimeReport ConfigOvertimeReport { get; set; }
        public ConfigAttendanceReport ConfigAttendanceReport { get; set; }

        public SysConfigReports()
        {
            this.ConfigOvertimeReport = new ConfigOvertimeReport
            {
                Mins = 30
            };

            this.ConfigDayOffReport = new ConfigDayOffReport()
            {
                TypeDayOffReport = TypeDayOffReport.Default,
            };

            this.ConfigAttendanceReport = new ConfigAttendanceReport()
            {
                TypeAttendanceReportPdf = TypeAttendanceReportPdf.Standard
            };
        }
    }

    public enum TypeDayOffReport
    {
        Default,
        New
    }

    public enum TypeAttendanceReportPdf
    {
        Standard,
        Compact
    }

    public enum TypeConfigReport
    {
        DayOffReport
    }

    public class ConfigDayOffReport
    {
        public TypeDayOffReport TypeDayOffReport { get; set; } = TypeDayOffReport.Default; 
    }

    public class ConfigAttendanceReport
    {
        public TypeAttendanceReportPdf TypeAttendanceReportPdf { get; set; } = TypeAttendanceReportPdf.Standard;
    }

    public class ConfigOvertimeReport
    {
        public int Mins { get; set; }
    }

    public class GetConfigReport
    {
        public TypeConfigReport TypeConfigReport { get; set; }
    }
}
