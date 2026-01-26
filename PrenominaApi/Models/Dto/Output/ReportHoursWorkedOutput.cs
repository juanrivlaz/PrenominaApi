namespace PrenominaApi.Models.Dto.Output
{
    public class ReportHoursWorkedOutput
    {
        public required int Code { get; set; }
        public required string FullName { get; set; }
        public required string Department { get; set; }
        public required string JobPosition { get; set; }
        public required TimeOnly CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public required DateOnly Date { get; set; }
        public required int HoursWorked { get; set; }
    }
}
