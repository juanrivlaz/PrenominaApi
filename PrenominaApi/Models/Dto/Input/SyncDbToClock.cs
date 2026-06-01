namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Sincroniza (escribe) los usuarios almacenados en la base de datos hacia un reloj.
    /// </summary>
    public class SyncDbToClock
    {
        public Guid ClockId { get; set; }
    }
}
