namespace PrenominaApi.Models.Dto.Input.Attendance
{
    public class GetAdditionalIncidents
    {
        public required int TypeNomina { get; set; }
        public required int NumPeriod { get; set; }
        public decimal Company { get; set; }
        public string? Tenant { get; set; }
    }
}
