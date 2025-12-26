namespace PrenominaApi.Models.Dto.Output
{
    public class WorkedDayOffs
    {
        public required decimal EmployeeCode { get; set; }
        public required string EmployeeName { get; set; }
        public required string EmployeeActivity { get; set; }
        public required decimal EmployeeSalary { get; set; }
        public required DateOnly Date { get; set; }
        public required string NumConcept { get; set; }
        public required int Hours { get; set; }
        public required decimal Amount { get; set; }
    }
}
