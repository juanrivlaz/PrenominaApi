namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para usar horas acumuladas para día de descanso
  /// </summary>
  public class UseOvertimeForRestDayInput
  {
    [Required]
    public int EmployeeCode { get; set; }

    /// <summary>
    /// Fecha del día de descanso a aplicar
    /// </summary>
    [Required]
    public DateOnly RestDate { get; set; }

    /// <summary>
    /// Minutos a usar del balance acumulado (debe ser igual o menor al balance disponible)
    /// </summary>
    [Required]
    public int MinutesToUse { get; set; }

    /// <summary>
    /// IDs de los movimientos de acumulación a usar (para trazabilidad de qué días se toman)
    /// </summary>
    public List<int>? SourceMovementIds { get; set; }

    /// <summary>
    /// Notas adicionales sobre el uso de horas acumuladas
    /// </summary>
    public string? Notes { get; set; }
  }
}