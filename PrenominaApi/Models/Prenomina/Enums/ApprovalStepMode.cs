namespace PrenominaApi.Models.Prenomina.Enums
{
    /// <summary>
    /// Cuántos firmantes del nivel se requieren para darlo por aprobado.
    /// </summary>
    public enum ApprovalStepMode
    {
        /// <summary>Basta con que firme uno de los candidatos del nivel.</summary>
        AnyOne = 1,

        /// <summary>Deben firmar todos los candidatos del nivel.</summary>
        All = 2,
    }
}
