namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Resultado de una operación de acumulación/pago
    /// </summary>
    public class OvertimeOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? MovementId { get; set; }
        public int NewBalance { get; set; }
        public string NewBalanceFormatted { get; set; } = string.Empty;
    }
}
