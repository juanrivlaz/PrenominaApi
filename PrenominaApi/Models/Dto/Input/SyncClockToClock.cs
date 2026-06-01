namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Sincroniza (copia) los usuarios de un reloj origen hacia un reloj destino.
    /// </summary>
    public class SyncClockToClock
    {
        public required Guid SourceClockId { get; set; }
        public required Guid TargetClockId { get; set; }
    }
}
