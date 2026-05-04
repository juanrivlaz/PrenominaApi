namespace PrenominaApi.Models.Dto
{
    public class SysConfigReports
    {
        public ConfigDayOffReport ConfigDayOffReport { get; set; }
        public ConfigOvertimeReport ConfigOvertimeReport { get; set; }
        public ConfigAttendanceReport ConfigAttendanceReport { get; set; }
        public ConfigSignatures ConfigSignatures { get; set; }
        public ConfigNameFormat ConfigNameFormat { get; set; }

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

            this.ConfigSignatures = new ConfigSignatures();
            this.ConfigNameFormat = new ConfigNameFormat();
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

    public enum NameOrder
    {
        FirstNameFirst = 0,
        LastNameFirst = 1
    }

    public class ConfigDayOffReport
    {
        public TypeDayOffReport TypeDayOffReport { get; set; } = TypeDayOffReport.Default;
    }

    public class ConfigAttendanceReport
    {
        public TypeAttendanceReportPdf TypeAttendanceReportPdf { get; set; } = TypeAttendanceReportPdf.Standard;
        public int CompactFontSize { get; set; } = 8;
        public bool ShowDayInitial { get; set; } = true;
    }

    public class ConfigOvertimeReport
    {
        public int Mins { get; set; }
    }

    public class ConfigSignatures
    {
        public List<SignatureItem> Signatures { get; set; } = new List<SignatureItem>();
    }

    public class SignatureItem
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }

    public class ConfigNameFormat
    {
        public NameOrder Order { get; set; } = NameOrder.FirstNameFirst;
    }

    public class GetConfigReport
    {
        public TypeConfigReport TypeConfigReport { get; set; }
    }
}
