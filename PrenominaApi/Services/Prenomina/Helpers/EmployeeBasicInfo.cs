namespace PrenominaApi.Services.Prenomina.Helpers
{
    internal class EmployeeBasicInfo
    {
        public int Code { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobPosition { get; set; } = string.Empty;
    }
}
