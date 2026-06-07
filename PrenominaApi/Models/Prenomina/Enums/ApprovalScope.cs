namespace PrenominaApi.Models.Prenomina.Enums
{
    /// <summary>
    /// Determina cómo se resuelve a la persona real que firma un nivel de aprobación,
    /// relativo al empleado solicitante.
    /// </summary>
    public enum ApprovalScope
    {
        /// <summary>
        /// Resuelto por el departamento del empleado: usuarios con el rol del nivel cuya
        /// asignación (user_department) incluye el centro/departamento del empleado.
        /// </summary>
        Department = 1,

        /// <summary>
        /// Resuelto a nivel empresa: cualquier usuario activo con el rol del nivel en la empresa
        /// (RH, Contralor, Director General).
        /// </summary>
        Company = 2,

        // Area = 3  // Pendiente: se modelará cuando se defina formalmente "área".
    }
}
