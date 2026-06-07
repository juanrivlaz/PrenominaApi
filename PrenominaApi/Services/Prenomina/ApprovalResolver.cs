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

                var dept = department.Trim();

                return (
                    from b in baseQuery
                    join ud in _context.userDepartments.AsNoTracking() on b.UserCompanyId equals ud.UserCompanyId
                    where ud.DepartmentCode.Trim() == dept
                    select b.UserId
                ).Distinct().ToList();
            }

            // Company: cualquier usuario con el rol en la empresa.
            return baseQuery.Select(b => b.UserId).Distinct().ToList();
        }
    }
}
