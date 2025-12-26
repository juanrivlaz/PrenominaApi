using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Output
{
    public class UserDetails
    {
        public required List<Company> Companies { get; set; }
        public IEnumerable<Center>? Centers { get; set; }
        public IEnumerable<Supervisor>? Supervisors { get; set; }
        public Role? role { get; set; }
    }
}
