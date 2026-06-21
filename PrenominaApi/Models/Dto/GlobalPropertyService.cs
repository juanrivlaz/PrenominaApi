using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto
{
    public class GlobalPropertyService
    {
        public int YearOfOperation { get; set; }
        public TypeTenant TypeTenant { get; set; }
        public string? UserId { get; set; }

        /// <summary>
        /// Centro/supervisor seleccionado (header "tenant"). "-999" (o "all") = TODOS.
        /// Para sudo significa "sin filtro por centro"; para no-sudo significa "todos los
        /// centros/supervisores asignados al usuario" (ver <see cref="RestrictToAssignedTenants"/>).
        /// </summary>
        public string Tenant { get; set; } = "-999";

        /// <summary>
        /// True cuando el usuario autenticado tiene rol sudo (acceso global, sin restricción de centro).
        /// </summary>
        public bool IsSudo { get; set; }

        /// <summary>
        /// Ids de los centros (departamentos) asignados al usuario. Sin normalizar.
        /// </summary>
        public List<string> AssignedCenterIds { get; set; } = new();

        /// <summary>
        /// Ids de los supervisores asignados al usuario.
        /// </summary>
        public List<decimal> AssignedSupervisorIds { get; set; } = new();

        /// <summary>
        /// True cuando el tenant seleccionado es "TODOS" ("-999"/"all").
        /// </summary>
        public bool IsAllTenants => Tenant == "-999" || Tenant == "all";

        /// <summary>
        /// True cuando hay que limitar "TODOS" a los centros/supervisores asignados del usuario
        /// (un no-sudo pidiendo TODOS). Para sudo es false: TODOS = sin restricción.
        /// </summary>
        public bool RestrictToAssignedTenants => IsAllTenants && !IsSudo;
    }
}
