namespace PrenominaApi.Models.Dto.Input
{
    public class GetTenantsUserByCompany
    {
        public required string UserId { get; set; }
        public int CompanyId { get; set; }
    }
}
