namespace PrenominaApi.Models.Dto.Input
{
    public class CreateUserCompanies
    {
        public required int Id { get; set; }
        public required IEnumerable<int> TenantIds { get; set; }
    }
}
