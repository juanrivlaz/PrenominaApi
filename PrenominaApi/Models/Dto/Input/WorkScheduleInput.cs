using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class WorkScheduleInput
    {
        [Required]
        public required string Label { get; set; }

        [Required]
        public required TimeOnly StartTime { get; set; }

        [Required]
        public required TimeOnly EndTime { get; set; }

        public TimeOnly? BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; }

        [Required]
        public required decimal WorkHours { get; set; }

        [Required]
        public bool IsNightShift { get; set; } = false;

        // Bitmap de días laborables. Bit 0 = Lunes ... Bit 6 = Domingo.
        public int WorkDays { get; set; } = 127;
    }
}
