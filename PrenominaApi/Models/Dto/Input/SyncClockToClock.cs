namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Sincroniza (copia) los usuarios de un reloj origen hacia un reloj destino.
    /// </summary>
    public class SyncClockToClock
    {
        public required Guid SourceClockId { get; set; }
        public required Guid TargetClockId { get; set; }

        /// <summary>
        /// Números de empleado (enroll) seleccionados para copiar.
        /// Si es null o está vacío se copian todos los usuarios del reloj origen.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
