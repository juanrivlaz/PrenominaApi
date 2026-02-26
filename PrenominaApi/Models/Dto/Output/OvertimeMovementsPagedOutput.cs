namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Respuesta paginada de movimientos
    /// </summary>
    public class OvertimeMovementsPagedOutput
    {
        public List<OvertimeMovementLogOutput> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}
