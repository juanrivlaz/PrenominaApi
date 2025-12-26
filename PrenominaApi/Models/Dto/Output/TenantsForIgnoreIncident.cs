using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output
{
    public class TenantsForIgnoreIncident
    {
        public TypeTenant TypeTenant { get; set; }
        public IEnumerable<Center>? Centers { get; set; }
        public IEnumerable<Supervisor>? Supervisors { get; set; }
    }
}
