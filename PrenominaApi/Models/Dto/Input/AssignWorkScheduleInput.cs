using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class AssignWorkScheduleInput
    {
        [Required]
        public required int[] EmployeeCodes { get; set; }

        [Required]
        public required Guid WorkScheduleId { get; set; }

        [Required]
        public required DateOnly EffectiveFrom { get; set; }
    }

    public class AssignActivityWorkScheduleInput
    {
        [Required]
        public required int ActivityId { get; set; }

        public Guid? WorkScheduleId { get; set; }
    }

    public class AssignEmployeeWorkScheduleInput
    {
        public Guid? WorkScheduleId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
    }
}
