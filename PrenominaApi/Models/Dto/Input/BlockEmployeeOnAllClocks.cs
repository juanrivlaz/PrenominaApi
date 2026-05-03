using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class BlockEmployeeOnAllClocks
    {
        [Required]
        public int EmployeeCode { get; set; }

        [Required]
        public bool Blocked { get; set; }
    }
}
