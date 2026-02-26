namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para procesar horas extras en lote
  /// </summary>
  public class ProcessOvertimesBatchInput
  {
    [Required]
    public int TypeNomina { get; set; }

    [Required]
    public int NumPeriod { get; set; }

    /// <summary>
    /// true = acumular, false = pagar directo
    /// </summary>
    [Required]
    public bool Accumulate { get; set; }

    /// <summary>
    /// Lista de códigos de empleado a procesar (vacío = todos)
    /// </summary>
    public List<int>? EmployeeCodes { get; set; }

    public string? Notes { get; set; }
  }
}