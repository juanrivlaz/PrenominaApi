using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;

namespace PrenominaApi.Services.Prenomina
{
    /// <summary>
    /// Resuelve la cadena de firmas (incident_approval_step) de un código de incidencia y
    /// la "congela" como niveles materializados (absence_request_approval) para una solicitud,
    /// resolviendo en ese momento a los usuarios candidatos según rol y alcance.
    /// </summary>
    public class ApprovalResolver
    {
        private readonly PrenominaDbContext _context;
        private readonly IBaseRepository<Key> _keyRepository;

        public ApprovalResolver(PrenominaDbContext context, IBaseRepository<Key> keyRepository)
        {
            _context = context;
            _keyRepository = keyRepository;
        }

        /// <summary>
        /// Crea los niveles de firma congelados para una solicitud de ausencia.
        /// Si el código no tiene cadena configurada, no hace nada (comportamiento previo).
        /// </summary>
        public void MaterializeForAbsenceRequest(Guid absenceRequestId, string incidentCode, int companyId, int employeeCode)
        {
            // La cadena se configura en el documento/contrato asignado al código de incidencia.
            // La empresa solo se usa para resolver a los candidatos reales de cada nivel.
            var documentId = _context.incidentCodes
                .AsNoTracking()
                .Where(c => c.Code == incidentCode)
                .Select(c => c.DocumentId)
                .FirstOrDefault();

            if (documentId == null)
            {
                return; // El código no tiene contrato/documento asignado: sin cadena.
            }

            var steps = _context.documentApprovalSteps
                .AsNoTracking()
                .Where(s => s.DocumentId == documentId.Value)
                .OrderBy(s => s.StepOrder)
                .ToList();

            if (steps.Count == 0)
            {
                return;
            }

            // Departamento (centro) del empleado para el alcance Department.
            var department = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(k => (int)k.Codigo == employeeCode && k.Company == companyId)
                .Select(k => k.Center)
                .FirstOrDefault()?
                .Trim();

            foreach (var step in steps)
            {
                var candidates = ResolveCandidates(step.RoleId, step.Scope, companyId, department);

                var status = candidates.Count > 0
                    ? ApprovalInstanceStatus.Pending
                    : (step.IsOptional ? ApprovalInstanceStatus.Skipped : ApprovalInstanceStatus.Blocked);

                _context.absenceRequestApprovals.Add(new AbsenceRequestApproval
                {
                    AbsenceRequestId = absenceRequestId,
                    StepOrder = step.StepOrder,
                    RoleId = step.RoleId,
                    Scope = step.Scope,
                    Mode = step.Mode,
                    IsOptional = step.IsOptional,
                    Status = status,
                    ResolvedCandidateUserIds = candidates.Count > 0 ? string.Join(",", candidates) : null,
                });
            }

            _context.SaveChanges();
        }

        /// <summary>
        /// Recalcula los candidatos de los niveles aún no firmados (Pending/Blocked) de una
        /// solicitud ya creada, sin recrearla. Útil tras corregir asignaciones de responsables.
        /// Devuelve cuántos niveles cambiaron.
        /// </summary>
        public int ReResolveForAbsenceRequest(Guid absenceRequestId, int companyId, int employeeCode)
        {
            var levels = _context.absenceRequestApprovals
                .Where(a => a.AbsenceRequestId == absenceRequestId)
                .ToList();

            if (levels.Count == 0)
            {
                return 0;
            }

            var department = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(k => (int)k.Codigo == employeeCode && k.Company == companyId)
                .Select(k => k.Center)
                .FirstOrDefault()?
                .Trim();

            var changed = 0;
            foreach (var level in levels)
            {
                // Solo niveles aún no resueltos; los aprobados/rechazados/omitidos no se tocan
                // (evita reactivar niveles fuera de orden).
                if (level.Status != ApprovalInstanceStatus.Pending && level.Status != ApprovalInstanceStatus.Blocked)
                {
                    continue;
                }

                var candidates = ResolveCandidates(level.RoleId, level.Scope, companyId, department);
                var newCsv = candidates.Count > 0 ? string.Join(",", candidates) : null;
                var newStatus = candidates.Count > 0
                    ? ApprovalInstanceStatus.Pending
                    : (level.IsOptional ? ApprovalInstanceStatus.Skipped : ApprovalInstanceStatus.Blocked);

                if (level.ResolvedCandidateUserIds != newCsv || level.Status != newStatus)
                {
                    level.ResolvedCandidateUserIds = newCsv;
                    level.Status = newStatus;
                    level.UpdatedAt = DateTime.UtcNow;
                    _context.absenceRequestApprovals.Update(level);
                    changed++;
                }
            }

            if (changed > 0)
            {
                _context.SaveChanges();
            }

            return changed;
        }

        /// <summary>
        /// Devuelve los usuarios que pueden firmar un nivel dado, según rol y alcance.
        /// </summary>
        public List<Guid> ResolveCandidates(Guid roleId, ApprovalScope scope, int companyId, string? department)
        {
            // Base: usuarios activos con el rol indicado y acceso a la empresa.
            var baseQuery =
                from uc in _context.userCompanies.AsNoTracking()
                join u in _context.users.AsNoTracking() on uc.UserId equals u.Id
                where uc.CompanyId == companyId && u.RoleId == roleId
                select new { UserCompanyId = uc.Id, UserId = u.Id };

            if (scope == ApprovalScope.Department)
            {
                if (string.IsNullOrWhiteSpace(department))
                {
                    return new List<Guid>();
                }

                var target = NormalizeDeptCode(department);

                // La comparación se hace en memoria porque el centro del empleado puede venir con
                // ceros a la izquierda (ej. '02') mientras que al usuario se le guarda como int
                // ('2'). El conjunto (usuarios con el rol en la empresa) es pequeño.
                var rows = (
                    from b in baseQuery
                    join ud in _context.userDepartments.AsNoTracking() on b.UserCompanyId equals ud.UserCompanyId
                    select new { b.UserId, ud.DepartmentCode }
                ).ToList();

                return rows
                    .Where(r => NormalizeDeptCode(r.DepartmentCode) == target)
                    .Select(r => r.UserId)
                    .Distinct()
                    .ToList();
            }

            // Company: cualquier usuario con el rol en la empresa.
            return baseQuery.Select(b => b.UserId).Distinct().ToList();
        }

        // Normaliza un código de departamento/centro para comparar de forma robusta:
        // si es numérico se elimina el padding de ceros ('02' -> '2'); si no, se compara
        // sin espacios y en mayúsculas.
        private static string NormalizeDeptCode(string? code)
        {
            var trimmed = (code ?? string.Empty).Trim();
            return int.TryParse(trimmed, out var n) ? n.ToString() : trimmed.ToUpperInvariant();
        }
    }
}
