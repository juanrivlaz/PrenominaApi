namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para ajuste manual de horas
  /// </summary>
  public class ManualOvertimeAdjustmentInput
  {
    [Required]
    public int EmployeeCode { get; set; }

    /// <summary>
    /// Minutos a ajustar (positivo para agregar, negativo para restar)
    /// </summary>
    [Required]
    public int Minutes { get; set; }

    [Required]
    public DateOnly ReferenceDate { get; set; }

    [Required]
    [MinLength(10)]
    public string Notes { get; set; } = string.Empty;
  }
}