using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output
{
    public class ResultLogin
    {
        public required string Token { get; set; }
        public string Username { get; set; } = string.Empty;
        public required UserDetails UserDetails { get; set; }
        public required TypeTenant TypeTenant { get; set; }
        public required int Year { get; set; }
    }
}
