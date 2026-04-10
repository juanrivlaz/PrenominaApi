namespace PrenominaApi.Models.Dto.Output.Attendance
{
    public class AdditionalIncidentOutput
    {
        public required string EmployeeName { get; set; }
        public required decimal EmployeeCode { get; set; }
        public required string EmployeeActivity { get; set; }
        public required DateOnly Date { get; set; }
        public required string IncidentCode { get; set; }
        public required string IncidentLabel { get; set; }
        public decimal? OperationValue { get; set; }
        public bool WithOperation { get; set; }
        public required string CreatedByUser { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
