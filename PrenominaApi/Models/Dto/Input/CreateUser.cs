using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateUser : HasPassword
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public required string Email { get; set; }
        public required Guid RoleId { get; set; }
        public IEnumerable<CreateUserCompanies>? Companies { get; set; }
    }
}
