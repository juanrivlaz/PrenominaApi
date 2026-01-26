using System.Collections.Immutable;

namespace PrenominaApi.Models.Prenomina.Enums
{
    public static class SysConfig
    {
        public const string YearOperation = "Year-Operation";
        public const string TypeTenant = "Type-Tenant";
        public const string AbsenteeismFactor = "Absenteeism-Factor";
        public const string ExtractChecks = "Extract-Checks";
        public const string Logo = "Logo";
        public static readonly ImmutableList<string> IncidentApplyToAttendance = ImmutableList.Create("F", "P", "S", "T");
        public const string UserDefault = "system@prenominaapi.com";
        public const string ConfigReports = "Config-Reports";
    }
}
