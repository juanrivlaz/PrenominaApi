namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Solicitud para eliminar un permiso (incidencia de tipo time-off) que aún no ha sido aprobado.
    /// Si el permiso pertenece a un grupo (asignación múltiple) se eliminan todos los días del grupo.
    /// </summary>
    public class DeletePermission
    {
        public required int EmployeeCode { get; set; }
        public required DateOnly Date { get; set; }
        public decimal CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
