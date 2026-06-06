namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Consumo de horas extra acumuladas para cubrir un día de permiso/ausencia.
    /// </summary>
    public class UseOvertimeForTimeOffInput
    {
        public int EmployeeCode { get; set; }

        /// <summary>
        /// Día del permiso al que se aplican las horas.
        /// </summary>
        public DateOnly TimeOffDate { get; set; }

        /// <summary>
        /// Minutos de horas acumuladas a consumir.
        /// </summary>
        public int MinutesToUse { get; set; }

        /// <summary>
        /// Incidencia (assistance_incident) asociada al permiso.
        /// </summary>
        public Guid AppliedIncidentId { get; set; }

        public string? Notes { get; set; }
    }
}
