using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Prenomina
{
  public class DeleteUser
  {
    [Required]
    public required string UserId {get; set;}
  }
}