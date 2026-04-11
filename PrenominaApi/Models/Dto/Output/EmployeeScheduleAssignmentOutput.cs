namespace PrenominaApi.Models.Dto.Output
{
    public class EmployeeScheduleAssignmentOutput
    {
        public Guid Id { get; set; }
        public int EmployeeCode { get; set; }
        public Guid WorkScheduleId { get; set; }
        public required string ScheduleLabel { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsNightShift { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }

    public class ActivityScheduleConfigOutput
    {
        public int ActivityId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public required string ScheduleLabel { get; set; }
        public bool IsNightShift { get; set; }
    }
}
