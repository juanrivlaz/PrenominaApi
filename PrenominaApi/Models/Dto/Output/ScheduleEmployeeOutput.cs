namespace PrenominaApi.Models.Dto.Output
{
    public class ScheduleEmployeeOutput
    {
        public required int Code { get; set; }
        public required TimeOnly CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public required DateOnly Date { get; set; }
        public required TimeOnly StartTime { get; set; }
        public required int MinsLate { get; set; }
    }
}
