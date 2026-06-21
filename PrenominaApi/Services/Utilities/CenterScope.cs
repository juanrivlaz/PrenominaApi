using PrenominaApi.Models.Dto;

namespace PrenominaApi.Services.Utilities
{
    /// <summary>
    /// Centraliza la resolución del alcance de centros/supervisores para el filtro "tenant".
    ///
    /// Semántica de "TODOS" ("-999"/"all"):
    ///  - sudo    => sin restricción (devuelve null): el servicio no filtra por centro/supervisor.
    ///  - no-sudo => restringe a los centros/supervisores ASIGNADOS al usuario.
    /// Para un tenant específico => restringe a ese único centro/supervisor.
    ///
    /// Convención de los conjuntos devueltos:
    ///  - null            => sin restricción (mostrar todo).
    ///  - conjunto vacío  => el usuario no tiene centros/supervisores asignados => no ve nada.
    ///  - conjunto lleno  => filtrar por esos valores.
    /// </summary>
    public static class CenterScope
    {
        /// <summary>
        /// Centros (normalizados) por los que se debe filtrar. null = sin restricción (sudo + TODOS).
        /// </summary>
        public static HashSet<string>? NormalizedCenterTargets(GlobalPropertyService gp, string? tenant = null)
        {
            var value = tenant ?? gp.Tenant;
            var isAll = value == "-999" || value == "all";

            if (!isAll)
            {
                return new HashSet<string> { TenantCode.Normalize(value) };
            }

            if (gp.IsSudo)
            {
                return null;
            }

            return gp.AssignedCenterIds
                .Select(TenantCode.Normalize)
                .ToHashSet();
        }

        /// <summary>
        /// Supervisores por los que se debe filtrar. null = sin restricción (sudo + TODOS).
        /// </summary>
        public static HashSet<decimal>? SupervisorTargets(GlobalPropertyService gp, string? tenant = null)
        {
            var value = tenant ?? gp.Tenant;
            var isAll = value == "-999" || value == "all";

            if (!isAll)
            {
                return new HashSet<decimal> { Convert.ToDecimal(value) };
            }

            if (gp.IsSudo)
            {
                return null;
            }

            return gp.AssignedSupervisorIds.ToHashSet();
        }

        /// <summary>
        /// Resuelve los códigos de empleado permitidos a partir de filas (Codigo, Center) ya
        /// materializadas en memoria, aplicando la normalización de centro.
        /// null = sin restricción por centro (sudo + TODOS).
        /// </summary>
        public static HashSet<int>? ResolveAllowedCenterCodes<T>(
            GlobalPropertyService gp,
            IEnumerable<T> keyRows,
            Func<T, decimal> codeSelector,
            Func<T, string?> centerSelector,
            string? tenant = null)
        {
            var targets = NormalizedCenterTargets(gp, tenant);
            if (targets == null)
            {
                return null;
            }

            return keyRows
                .Where(r => targets.Contains(TenantCode.Normalize(centerSelector(r))))
                .Select(r => (int)codeSelector(r))
                .ToHashSet();
        }
    }
}
