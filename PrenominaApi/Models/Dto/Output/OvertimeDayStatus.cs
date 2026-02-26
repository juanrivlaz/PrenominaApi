namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Estado del procesamiento de tiempo extra de un día
    /// </summary>
    public enum OvertimeDayStatus
    {
        Pending = 0,
        Accumulated = 1,
        Paid = 2,
        Cancelled = 3
    }
}
