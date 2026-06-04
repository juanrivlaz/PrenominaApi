namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Cuerpo de la petición para sincronizar usuarios seleccionados.
    /// </summary>
    public class SyncSelectedUsers
    {
        /// <summary>
        /// Números de empleado (enroll) seleccionados. Si es null o está vacío
        /// se sincronizan todos los usuarios.
        /// </summary>
        public List<string>? EnrollNumbers { get; set; }
    }
}
