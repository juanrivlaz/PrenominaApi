namespace PrenominaApi.Models.Dto.Output
{
    public class SyncIncapacityOutput
    {
        public int TotalIncapacities { get; set; }
        public int TotalVacations { get; set; }
        public required IEnumerable<EmployeeDayOffOutput> Items { get; set; }
    }
}
