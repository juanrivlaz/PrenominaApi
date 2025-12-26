using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreatePeriodLite
    {
        public required int NumPeriod { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly ClosingDate { get; set; }
        public DateOnly? DatePayment { get; set; }
        public required DateOnly StartAdminDate { get; set; }
        public required DateOnly ClosingAdminDate { get; set; }
    }

    public class CreatePeriods
    {
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public required int TypePayroll { get; set; }
        public required IEnumerable<CreatePeriodLite> Dates { get; set; }
    }

    public class CreatePeriodsByFile
    {
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public required int TypePayroll { get; set; }
        public required IFormFile File { get; set; }
    }
}
