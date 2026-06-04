namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Sincroniza (escribe) los usuarios almacenados en la base de datos hacia un reloj.
    /// </summary>
    public class SyncDbToClock
    {
        public Guid ClockId { get; set; }

        /// <summary>
        /// Números de empleado (enroll) seleccionados para sincronizar.
        /// Si es null o está vacío se sincronizan todos los usuarios.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
