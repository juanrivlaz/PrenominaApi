using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class ContractsInput
    {
        public TypeTenant TypeTenant { get; set; }
        public required string Tenant { get; set; }
        public decimal CompanyId { get; set; }
        public int TypeNom { get; set; }
        public bool? IgnoreNotAction { get; set; }
        public string? UserId { get; set; }
    }
}
