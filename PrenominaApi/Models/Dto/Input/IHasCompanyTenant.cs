namespace PrenominaApi.Models.Dto.Input
{
    public interface IHasCompanyTenant
    {
        decimal Company { get; set; }
        string Tenant { get; set; }
    }
}
