namespace PrenominaApi.Models.Dto.Output
{
    public class EmployeeOutput : Employee
    {
        public required string Activity { get; set; }
        public required string TenantName { get; set; }
    }
}
