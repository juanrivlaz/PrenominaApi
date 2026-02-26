namespace PrenominaApi.Models.Prenomina.Enums
{
    /// <summary>
    /// Tipos de movimiento para el log de horas extras
    /// </summary>
    public enum OvertimeMovementType
    {
        /// <summary>
        /// Horas acumuladas desde tiempo extra trabajado
        /// </summary>
        Accumulation = 1,

        /// <summary>
        /// Horas usadas para día de descanso
        /// </summary>
        UsedForRestDay = 2,

        /// <summary>
        /// Horas pagadas directamente (sin acumular)
        /// </summary>
        DirectPayment = 3,

        /// <summary>
        /// Ajuste manual (corrección administrativa)
        /// </summary>
        ManualAdjustment = 4,

        /// <summary>
        /// Cancelación de movimiento previo
        /// </summary>
        Cancellation = 5
    }
}
