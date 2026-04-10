namespace PrenominaApi.Models.Dto.Output
{
    public class ReportAbandonmentOutput
    {
        public required int Code { get; set; }
        public required string FullName { get; set; }
        public required string Department { get; set; }
        public required string JobPosition { get; set; }
        public required int ConsecutiveDays { get; set; }
        public required DateOnly StartDate { get; set; }
        public required DateOnly EndDate { get; set; }
    }
}
