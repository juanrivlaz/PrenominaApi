using Microsoft.AspNetCore.Http;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public enum ApprovalOutcome
    {
        /// <summary>El nivel avanzó/se firmó pero la solicitud sigue pendiente.</summary>
        Partial,
        /// <summary>Se firmó el último nivel: la solicitud queda aprobada.</summary>
        Approved,
        /// <summary>Se rechazó: la solicitud queda rechazada.</summary>
        Rejected,
    }

    /// <summary>
    /// Progresión genérica de una cadena de firmas (independiente del tipo de solicitud).
    /// Firma/rechaza el nivel pendiente más temprano del que el usuario actual es candidato
    /// (directo o como suplente), maneja modo AnyOne/All y avanza. NO aplica efectos del host
    /// (estado de la solicitud, propagación a incidencias, reintegro, etc.): eso lo hace el llamador.
    /// </summary>
    public class ApprovalFlowService
    {
        private readonly IBaseRepositoryPrenomina<AbsenceRequestApproval> _approvalRepository;
        private readonly IBaseRepositoryPrenomina<ApproverDelegation> _delegationRepository;
        private readonly GlobalPropertyService _globalPropertyService;

        public ApprovalFlowService(
            IBaseRepositoryPrenomina<AbsenceRequestApproval> approvalRepository,
            IBaseRepositoryPrenomina<ApproverDelegation> delegationRepository,
            GlobalPropertyService globalPropertyService)
        {
            _approvalRepository = approvalRepository;
            _delegationRepository = delegationRepository;
            _globalPropertyService = globalPropertyService;
        }

        public ApprovalOutcome Advance(Guid requestId, bool approve, string? comment)
        {
            var now = DateTime.UtcNow;
            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;
            if (currentUserId == null)
            {
                throw new BadHttpRequestException("No se pudo identificar al usuario.");
            }

            var chain = _approvalRepository
                .GetByFilter(a => a.AbsenceRequestId == requestId)
                .OrderBy(a => a.StepOrder)
                .ToList();

            if (chain.Count == 0)
            {
                throw new BadHttpRequestException("La solicitud no tiene cadena de firmas.");
            }

            var currentLevel = chain.FirstOrDefault(l =>
                l.Status == ApprovalInstanceStatus.Pending || l.Status == ApprovalInstanceStatus.Blocked);

            if (currentLevel == null)
            {
                throw new BadHttpRequestException("La solicitud ya no tiene niveles pendientes por firmar.");
            }

            if (currentLevel.Status == ApprovalInstanceStatus.Blocked)
            {
                throw new BadHttpRequestException("El nivel actual no tiene responsables asignados. Contacte al administrador.");
            }

            var candidates = ParseCandidateIds(currentLevel.ResolvedCandidateUserIds);
            var delegators = GetDelegatorIdsFor(currentUserId.Value);
            var signableFor = candidates
                .Where(c => c == currentUserId.Value || delegators.Contains(c))
                .ToHashSet();

            if (signableFor.Count == 0)
            {
                throw new BadHttpRequestException("No tienes autorización para firmar este nivel de la solicitud.");
            }

            if (!approve)
            {
                currentLevel.Status = ApprovalInstanceStatus.Rejected;
                currentLevel.ApprovedByUserId = currentUserId;
                currentLevel.ApprovedAt = now;
                currentLevel.Comment = comment;
                currentLevel.UpdatedAt = now;
                _approvalRepository.Update(currentLevel);
                _approvalRepository.Save();
                return ApprovalOutcome.Rejected;
            }

            // Registra los candidatos cubiertos por esta firma (usuario y/o titulares que suple).
            var signed = ParseCandidateIds(currentLevel.SignedUserIds);
            foreach (var c in signableFor)
            {
                signed.Add(c);
            }
            currentLevel.SignedUserIds = string.Join(",", signed);

            // Modo All: el nivel se aprueba sólo cuando TODOS los candidatos firmaron.
            var levelCompleted = currentLevel.Mode == ApprovalStepMode.All
                ? candidates.All(c => signed.Contains(c))
                : true;

            if (!levelCompleted)
            {
                currentLevel.Comment = comment;
                currentLevel.UpdatedAt = now;
                _approvalRepository.Update(currentLevel);
                _approvalRepository.Save();
                return ApprovalOutcome.Partial;
            }

            currentLevel.Status = ApprovalInstanceStatus.Approved;
            currentLevel.ApprovedByUserId = currentUserId;
            currentLevel.ApprovedAt = now;
            currentLevel.Comment = comment;
            currentLevel.UpdatedAt = now;
            _approvalRepository.Update(currentLevel);
            _approvalRepository.Save();

            var hasMorePending = chain.Any(l => l.Id != currentLevel.Id &&
                (l.Status == ApprovalInstanceStatus.Pending || l.Status == ApprovalInstanceStatus.Blocked));

            return hasMorePending ? ApprovalOutcome.Partial : ApprovalOutcome.Approved;
        }

        public HashSet<Guid> GetDelegatorIdsFor(Guid delegateUserId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return _delegationRepository
                .GetByFilter(d => d.DelegateUserId == delegateUserId
                    && d.FromDate <= today
                    && (d.ToDate == null || d.ToDate >= today))
                .Select(d => d.UserId)
                .ToHashSet();
        }

        public static HashSet<Guid> ParseCandidateIds(string? csv)
        {
            var set = new HashSet<Guid>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return set;
            }

            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var id))
                {
                    set.Add(id);
                }
            }

            return set;
        }
    }
}
