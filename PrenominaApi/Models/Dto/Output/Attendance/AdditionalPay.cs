namespace PrenominaApi.Models.Dto.Output.Attendance
{
    public class AdditionalPay
    {
        public required string EmployeeName { get; set; }
        public required decimal EmployeeCode { get; set; }
        public required string EmployeeActivity { get; set; }
        public required string Company { get; set; }
        public required DateOnly Date { get; set; }
        public required string IncidentCode { get; set; }
        public required string Column { get; set; }
        public required decimal BaseValue { get; set; }
        public required string Operator { get; set; }
        public required string OperatorText { get; set; }
        public required decimal OperationValue { get; set; }
        public required decimal Total { get; set; }
    }
}
