using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Prenomina
{
    [Table("clock_attendace")]
    public class ClockAttendance
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("user_id")]
        [Required]
        public required Guid ClockId { get; set; }
        [Column("enroll_number")]
        [Required]
        public required string EnrollNumber { get; set; }
        [Column("verify_mode")]
        [Required]
        public required int VerifyMode { get; set; }
        [Column("in_out_mode")]
        [Required]
        public required int InOutMode { get; set; }
        [Column("year")]
        [Required]
        public required int Year { get; set; }
        [Column("month")]
        [Required]
        public required int Month { get; set; }
        [Column("day")]
        [Required]
        public required int Day { get; set; }
        [Column("hour")]
        [Required]
        public required int Hour { get; set; }
        [Column("minute")]
        [Required]
        public required int Minute { get; set; }
        [Column("second")]
        [Required]
        public required int Second { get; set; }
        [Column("work_code")]
        [Required]
        public required int WorkCode { get; set; }
    }
}
