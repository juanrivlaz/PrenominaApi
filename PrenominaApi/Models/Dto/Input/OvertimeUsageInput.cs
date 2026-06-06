namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Horas extra acumuladas a utilizar en un día específico de un permiso.
    /// </summary>
    public class OvertimeUsageInput
    {
        public DateOnly Date { get; set; }

        /// <summary>
        /// Minutos de horas acumuladas a consumir para ese día.
        /// </summary>
        public int Minutes { get; set; }
    }
}
