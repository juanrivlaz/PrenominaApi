namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para pagar horas extras directamente (sin acumular)
  /// </summary>
  public class PayOvertimeDirectInput
  {
    [Required]
    public int EmployeeCode { get; set; }

    [Required]
    public DateOnly SourceDate { get; set; }

    [Required]
    public int Minutes { get; set; }

    public TimeOnly? CheckIn { get; set; }

    public TimeOnly? CheckOut { get; set; }

    public string? Notes { get; set; }
  }
}