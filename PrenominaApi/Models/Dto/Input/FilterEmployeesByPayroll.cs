namespace PrenominaApi.Models.Dto.Input
{
    public class FilterEmployeesByPayroll : Paginator
    {
        public int TypeNom { get; set; }
        public int CompanyId { get; set; }
        public string? Tenant { get; set; }
    }
}
