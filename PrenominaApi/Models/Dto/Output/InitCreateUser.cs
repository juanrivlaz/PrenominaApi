using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Output
{
    public class InitCreateUser : UserDetails
    {
        public required IEnumerable<Role> roles { get; set; }
    }
}
