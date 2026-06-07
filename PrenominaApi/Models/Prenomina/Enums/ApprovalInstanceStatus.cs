namespace PrenominaApi.Models.Prenomina.Enums
{
    /// <summary>
    /// Estado de un nivel de firma dentro de la cadena de aprobación de una solicitud.
    /// </summary>
    public enum ApprovalInstanceStatus
    {
        /// <summary>Aún no se ha firmado.</summary>
        Pending = 0,

        /// <summary>Firmado/aprobado.</summary>
        Approved = 1,

        /// <summary>Rechazado en este nivel (detiene la cadena).</summary>
        Rejected = 2,

        /// <summary>Nivel opcional sin candidatos: se omite automáticamente.</summary>
        Skipped = 3,

        /// <summary>Nivel obligatorio sin candidatos resolubles: requiere intervención.</summary>
        Blocked = 4,
    }
}
