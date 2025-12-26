namespace PrenominaApi.Models.Dto.Input
{
    public class IgnoreIncident
    {
        public required string Code { get; set; }
        public required bool Ignore { get; set; }
    }
}
