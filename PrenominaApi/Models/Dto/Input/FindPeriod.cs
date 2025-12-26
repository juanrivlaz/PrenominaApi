namespace PrenominaApi.Models.Dto.Input
{
    public class FindPeriod
    {
        public required DateOnly Date { get; set; }
        public required int TypePayroll { get; set; }
        public required int Year { get; set; }
        public required int CompanyId { get; set; }
    }
}
