namespace PrenominaApi.Models.Dto.Output
{
    public class AbandonmentCheckDateRow
    {
        public required int EmployeeCode { get; set; }
        public required DateOnly Date { get; set; }
    }
}
