using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Output.IncidentCodes
{
    public class InitForCreateIncidentCode
    {
        public required IEnumerable<User> Users { get; set; }
        public required IEnumerable<Role> Roles { get; set; }
    }
}
