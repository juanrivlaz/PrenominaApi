namespace PrenominaApi.Models.Dto.Output
{
    public class WorkScheduleOutput
    {
        public Guid Id { get; set; }
        public required string Label { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeOnly? BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; }
        public decimal WorkHours { get; set; }
        public bool IsNightShift { get; set; }
    }
}
