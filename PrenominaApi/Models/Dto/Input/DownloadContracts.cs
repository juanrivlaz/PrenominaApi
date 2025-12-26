namespace PrenominaApi.Models.Dto.Input
{
    public class DownloadContracts
    {
        public decimal Company { get; set; }
        public required string Tenant { get; set; }
        public string? UserId { get; set; }
    }
}
