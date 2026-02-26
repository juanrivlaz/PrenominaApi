using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
  /// <summary>
  /// Input para obtener historial de movimientos
  /// </summary>
  public class GetOvertimeMovementsInput
  {
    public int? EmployeeCode { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public OvertimeMovementType? MovementType { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 30;
  }
}