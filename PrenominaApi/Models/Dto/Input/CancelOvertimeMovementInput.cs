namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para cancelar un movimiento previo
  /// </summary>
  public class CancelOvertimeMovementInput
  {
    [Required]
    public int MovementId { get; set; }

    [Required]
    [MinLength(10)]
    public string Reason { get; set; } = string.Empty;
  }
}