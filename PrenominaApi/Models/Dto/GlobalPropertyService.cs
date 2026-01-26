using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto
{
    public class GlobalPropertyService
    {
        public int YearOfOperation { get; set; }
        public TypeTenant TypeTenant { get; set; }
        public string? UserId { get; set; }
    }
}
