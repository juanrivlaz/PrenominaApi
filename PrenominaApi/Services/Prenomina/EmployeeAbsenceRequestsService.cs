using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.EmployeeAbsenceRequest;
using PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Utilities;
using PrenominaApi.Services.Utilities.PermissionPdf;

namespace PrenominaApi.Services.Prenomina
{
    public class EmployeeAbsenceRequestsService : ServicePrenomina<EmployeeAbsenceRequests>
    {
        public readonly IBaseRepository<Key> _keyRepository;
        public readonly IBaseRepository<Company> _companyRepository;
        private readonly PermissionPdfService _permissionPdfService;
        public readonly GlobalPropertyService _globalPropertyService;
        private readonly IBaseServicePrenomina<SystemConfig> _sysConfigService;
        private readonly IBaseRepositoryPrenomina<AssistanceIncident> _assistanceIncidentRepository;
        private readonly IBaseRepositoryPrenomina<IncidentApprover> _incidentApproverRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncidentApprover> _assistanceIncidentApproverRepository;
        private readonly IBaseRepositoryPrenomina<OvertimeMovementLog> _overtimeMovementLogRepository;
        private readonly IBaseRepositoryPrenomina<AbsenceRequestApproval> _absenceRequestApprovalRepository;
        private readonly IBaseRepositoryPrenomina<Role> _roleRepository;
        private readonly IBaseRepositoryPrenomina<User> _userRepository;
        private readonly IBaseRepositoryPrenomina<ApproverDelegation> _approverDelegationRepository;
        private readonly IBaseRepositoryPrenomina<Document> _documentRepository;
        private readonly DocumentPdfRenderer _documentPdfRenderer;
        private readonly ApprovalResolver _approvalResolver;

        public EmployeeAbsenceRequestsService(
            IBaseRepositoryPrenomina<EmployeeAbsenceRequests> repository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Company> companyRepository,
            GlobalPropertyService globalPropertyService,
            PermissionPdfService permissionPdfService,
            IBaseServicePrenomina<SystemConfig> sysConfigService,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncidentRepository,
            IBaseRepositoryPrenomina<IncidentApprover> incidentApproverRepository,
            IBaseRepositoryPrenomina<AssistanceIncidentApprover> assistanceIncidentApproverRepository,
            IBaseRepositoryPrenomina<OvertimeMovementLog> overtimeMovementLogRepository,
            IBaseRepositoryPrenomina<AbsenceRequestApproval> absenceRequestApprovalRepository,
            IBaseRepositoryPrenomina<Role> roleRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseRepositoryPrenomina<ApproverDelegation> approverDelegationRepository,
            IBaseRepositoryPrenomina<Document> documentRepository,
            DocumentPdfRenderer documentPdfRenderer,
            ApprovalResolver approvalResolver
        ) : base(repository)
        {
            _keyRepository = keyRepository;
            _companyRepository = companyRepository;
            _globalPropertyService = globalPropertyService;
            _permissionPdfService = permissionPdfService;
            _sysConfigService = sysConfigService;
            _assistanceIncidentRepository = assistanceIncidentRepository;
            _incidentApproverRepository = incidentApproverRepository;
            _assistanceIncidentApproverRepository = assistanceIncidentApproverRepository;
            _overtimeMovementLogRepository = overtimeMovementLogRepository;
            _absenceRequestApprovalRepository = absenceRequestApprovalRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _approverDelegationRepository = approverDelegationRepository;
            _documentRepository = documentRepository;
            _documentPdfRenderer = documentPdfRenderer;
            _approvalResolver = approvalResolver;
        }

        public IEnumerable<EmployeeAbsenceRequestOutput> ExecuteProcess(decimal companyId)
        {
            var requests = _repository.GetContextEntity().Include(e => e.IncidentCodeItem).Where(e => e.CompanyId == companyId).ToList();
            var employeeCodes = requests.Select(r => r.EmployeeCode).Distinct().ToList();
            var keyEmployee = _keyRepository.GetContextEntity().Include(k => k.Tabulator).Include(k => k.Employee);

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keyEmployee.Include(k => k.CenterItem);
            }
            else
            {
                keyEmployee.Include(k => k.SupervisorItem);
            }

            var keys = keyEmployee.Where(k => k.Company == companyId && employeeCodes.Contains((int)k.Codigo)).ToList();

            // Filtrar por el centro/supervisor seleccionado (a menos que sea "TODOS" = -999).
            var tenant = _globalPropertyService.Tenant;
            if (!string.IsNullOrEmpty(tenant) && tenant != "-999" && tenant != "all")
            {
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    // El centro del empleado puede venir con ceros a la izquierda ('04') mientras
                    // que el tenant del header llega como int ('4'); se normalizan ambos lados
                    // (igual que ApprovalResolver al resolver candidatos) para que matcheen.
                    var target = TenantCode.Normalize(tenant);
                    keys = keys.Where(k => TenantCode.Normalize(k.Center) == target).ToList();
                }
                else
                {
                    var supervisorId = Convert.ToDecimal(tenant);
                    keys = keys.Where(k => k.Supervisor == supervisorId).ToList();
                }

                var allowedCodes = keys.Select(k => (int)k.Codigo).ToHashSet();
                requests = requests.Where(r => allowedCodes.Contains(r.EmployeeCode)).ToList();
                employeeCodes = requests.Select(r => r.EmployeeCode).Distinct().ToList();
            }

            // ===== Multi-approval progress =====
            // Approvers configured per incidence code present in the requests.
            var incidentCodes = requests.Select(r => r.IncidentCode).Distinct().ToList();
            var approversByCode = _incidentApproverRepository
                .GetByFilter(ia => incidentCodes.Contains(ia.IncidentCode))
                .GroupBy(ia => ia.IncidentCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Incidences related to the requests (by company, employee and code) to count the
            // approvals already registered (the incidence approvers mechanism is reused).
            // The date-range matching is done in memory.
            var relatedIncidents = _assistanceIncidentRepository
                .GetByFilter(ai => ai.CompanyId == companyId && employeeCodes.Contains(ai.EmployeeCode) && incidentCodes.Contains(ai.IncidentCode))
                .ToList();

            var relatedIncidentIds = relatedIncidents.Select(i => i.Id).ToHashSet();
            var approvalsByIncident = _assistanceIncidentApproverRepository
                .GetByFilter(a => relatedIncidentIds.Contains(a.AssistanceIncidentId))
                .GroupBy(a => a.AssistanceIncidentId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.IncidentApproverId).Distinct().ToList());

            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;

            // Titulares a los que el usuario actual puede suplir hoy (delegación vigente).
            var currentUserDelegators = currentUserId != null
                ? GetDelegatorIdsFor(currentUserId.Value)
                : new HashSet<Guid>();

            // Cadenas de firmas congeladas por solicitud (flujo nuevo). Si una solicitud tiene
            // cadena, su progreso/permiso se calcula desde aquí; si no, se usa el flujo previo.
            var requestIds = requests.Select(r => r.Id).ToList();
            var chainsByRequest = _absenceRequestApprovalRepository
                .GetByFilter(a => requestIds.Contains(a.AbsenceRequestId))
                .ToList()
                .GroupBy(a => a.AbsenceRequestId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StepOrder).ToList());

            var result = requests.Select(r =>
            {
                var key = keys.FirstOrDefault(k => k.Codigo == r.EmployeeCode);
                var employee = key?.Employee;
                var activity = key?.Tabulator.Activity;
                var incident = r.IncidentCodeItem;

                bool requiresApproval;
                int totalApprovers;
                int approvedCount;
                bool alreadyApprovedByMe;
                bool canApprove;

                if (chainsByRequest.TryGetValue(r.Id, out var chain) && chain.Count > 0)
                {
                    // ===== Progreso por cadena de firmas =====
                    var effectiveLevels = chain.Where(l => l.Status != ApprovalInstanceStatus.Skipped).ToList();
                    var currentLevel = chain.FirstOrDefault(l => l.Status == ApprovalInstanceStatus.Pending);
                    // Puede firmar si es candidato directo o suplente de algún candidato, y aún
                    // no ha firmado este nivel (relevante en modo All).
                    var currentCandidates = currentLevel != null ? ParseCandidateIds(currentLevel.ResolvedCandidateUserIds) : new HashSet<Guid>();
                    var currentSigned = currentLevel != null ? ParseCandidateIds(currentLevel.SignedUserIds) : new HashSet<Guid>();
                    var iCanSignCurrent = currentLevel != null && currentUserId != null
                        && currentCandidates.Any(c => (c == currentUserId.Value || currentUserDelegators.Contains(c)) && !currentSigned.Contains(c));
                    var iAmCurrentCandidate = iCanSignCurrent;
                    var iApprovedSomeLevel = currentUserId != null
                        && chain.Any(l => l.Status == ApprovalInstanceStatus.Approved && l.ApprovedByUserId == currentUserId);

                    requiresApproval = true;
                    totalApprovers = effectiveLevels.Count;
                    approvedCount = effectiveLevels.Count(l => l.Status == ApprovalInstanceStatus.Approved);
                    alreadyApprovedByMe = iApprovedSomeLevel && !iAmCurrentCandidate;
                    canApprove = iAmCurrentCandidate;
                }
                else
                {
                    // ===== Flujo previo (incident_approver plano) =====
                    approversByCode.TryGetValue(r.IncidentCode, out var codeApprovers);
                    totalApprovers = codeApprovers?.Count ?? 0;
                    var approverIds = codeApprovers?.Select(a => a.Id).ToHashSet() ?? new HashSet<Guid>();

                    var requestIncidentIds = relatedIncidents
                        .Where(ai => ai.EmployeeCode == r.EmployeeCode && ai.IncidentCode == r.IncidentCode && ai.Date >= r.StartDate && ai.Date <= r.EndDate)
                        .Select(ai => ai.Id)
                        .ToList();

                    var approvedApproverIds = requestIncidentIds
                        .SelectMany(id => approvalsByIncident.TryGetValue(id, out var list) ? list : Enumerable.Empty<Guid>())
                        .Where(id => approverIds.Contains(id))
                        .Distinct()
                        .ToList();

                    var myApprover = codeApprovers?.FirstOrDefault(a => currentUserId != null && a.UserId == currentUserId);

                    requiresApproval = totalApprovers > 0;
                    approvedCount = approvedApproverIds.Count;
                    alreadyApprovedByMe = myApprover != null && approvedApproverIds.Contains(myApprover.Id);
                    canApprove = totalApprovers == 0 || myApprover != null;
                }

                return new EmployeeAbsenceRequestOutput
                {
                    Id = r.Id,
                    EmployeeName = $"{employee?.Name ?? string.Empty} {employee?.LastName ?? string.Empty} {employee?.MLastName ?? string.Empty}",
                    EmployeeCode = r.EmployeeCode,
                    EmployeeActivity = activity ?? string.Empty,
                    IncidentCode = r.IncidentCode,
                    IncidentDescription = incident?.Label ?? string.Empty,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    Notes = r.Notes,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    RequiresApproval = requiresApproval,
                    TotalApprovers = totalApprovers,
                    ApprovedCount = approvedCount,
                    AlreadyApprovedByMe = alreadyApprovedByMe,
                    CanApprove = canApprove,
                };
            });

            return result;
        }

        public EmployeeAbsenceRequestDetailOutput ExecuteProcess(GetAbsenceRequestDetail input)
        {
            if (string.IsNullOrEmpty(input.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetContextEntity()
                .Include(e => e.IncidentCodeItem)
                .FirstOrDefault(e => e.Id == Guid.Parse(input.Id));

            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            var key = _keyRepository.GetContextEntity()
                .Include(k => k.Tabulator)
                .Include(k => k.Employee)
                .FirstOrDefault(k => k.Company == item.CompanyId && (int)k.Codigo == item.EmployeeCode);
            var employee = key?.Employee;

            // Incidencias relacionadas (mismo empleado/código dentro del rango de fechas).
            var relatedIncidents = GetRelatedIncidents(item);
            var relatedIncidentIds = relatedIncidents.Select(i => i.Id).ToList();

            // ===== Progreso de aprobaciones =====
            var approvers = _incidentApproverRepository.GetByFilter(ia => ia.IncidentCode == item.IncidentCode).ToList();
            var totalApprovers = approvers.Count;
            var approverIds = approvers.Select(a => a.Id).ToHashSet();
            var approvedCount = relatedIncidentIds.Count == 0 ? 0 : _assistanceIncidentApproverRepository
                .GetByFilter(a => relatedIncidentIds.Contains(a.AssistanceIncidentId) && approverIds.Contains(a.IncidentApproverId))
                .Select(a => a.IncidentApproverId)
                .Distinct()
                .Count();

            // ===== Cadena de firmas (flujo nuevo) =====
            var chain = _absenceRequestApprovalRepository
                .GetByFilter(a => a.AbsenceRequestId == item.Id)
                .OrderBy(a => a.StepOrder)
                .ToList();

            var approvalChain = new List<AbsenceRequestApprovalStepOutput>();
            if (chain.Count > 0)
            {
                var roleIds = chain.Select(c => c.RoleId).Distinct().ToList();
                var roleLabels = _roleRepository.GetByFilter(r => roleIds.Contains(r.Id))
                    .ToDictionary(r => r.Id, r => r.Label);

                // Nombres de TODOS los usuarios involucrados: quienes firmaron y los candidatos
                // asignados (snapshot) de cada nivel, para mostrar a quién está asignado.
                var candidatesByLevel = chain.ToDictionary(c => c.Id, c => ParseCandidateIds(c.ResolvedCandidateUserIds).ToList());
                var involvedUserIds = chain.Where(c => c.ApprovedByUserId != null).Select(c => c.ApprovedByUserId!.Value)
                    .Concat(candidatesByLevel.Values.SelectMany(v => v))
                    .Distinct()
                    .ToList();
                var userNames = involvedUserIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : _userRepository.GetByFilter(u => involvedUserIds.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u.Name);
                var approverNames = userNames;

                var currentLevel = chain.FirstOrDefault(l => l.Status == ApprovalInstanceStatus.Pending);

                // "En espera desde": la última firma previa, o la creación de la solicitud.
                const int overdueThresholdDays = 3;
                var lastApprovedAt = chain
                    .Where(l => l.Status == ApprovalInstanceStatus.Approved && l.ApprovedAt != null)
                    .Select(l => l.ApprovedAt!.Value)
                    .DefaultIfEmpty(item.CreatedAt)
                    .Max();
                var daysPending = currentLevel != null ? (int)(DateTime.UtcNow.Date - lastApprovedAt.Date).TotalDays : (int?)null;

                approvalChain = chain.Select(c => new AbsenceRequestApprovalStepOutput
                {
                    StepOrder = c.StepOrder,
                    RoleLabel = roleLabels.TryGetValue(c.RoleId, out var label) ? label : "Rol",
                    Scope = c.Scope.ToString(),
                    Status = c.Status.ToString(),
                    IsCurrent = currentLevel != null && c.Id == currentLevel.Id,
                    ApprovedByName = c.ApprovedByUserId != null && approverNames.TryGetValue(c.ApprovedByUserId.Value, out var name) ? name : null,
                    ApprovedAt = c.ApprovedAt,
                    Comment = c.Comment,
                    DaysPending = currentLevel != null && c.Id == currentLevel.Id ? daysPending : null,
                    IsOverdue = currentLevel != null && c.Id == currentLevel.Id && daysPending != null && daysPending >= overdueThresholdDays,
                    CandidateNames = candidatesByLevel.TryGetValue(c.Id, out var cands)
                        ? cands.Select(id => userNames.TryGetValue(id, out var n) ? n : null).Where(n => n != null).Cast<string>().ToList()
                        : new List<string>(),
                }).ToList();

                // El progreso refleja la cadena cuando existe.
                var effectiveLevels = chain.Where(l => l.Status != ApprovalInstanceStatus.Skipped).ToList();
                totalApprovers = effectiveLevels.Count;
                approvedCount = effectiveLevels.Count(l => l.Status == ApprovalInstanceStatus.Approved);
            }

            // ===== Consumo de horas extra acumuladas por día =====
            // Movimientos de tipo "usado en permiso" aplicados a las incidencias del permiso,
            // excluyendo los que ya fueron reintegrados (tienen una Cancelación que los referencia).
            var overtimeByDate = new Dictionary<DateOnly, int>();
            if (relatedIncidentIds.Count > 0)
            {
                var usageMovements = _overtimeMovementLogRepository
                    .GetByFilter(m => m.CompanyId == (int)item.CompanyId &&
                        m.MovementType == OvertimeMovementType.UsedForTimeOff &&
                        m.AppliedIncidentId != null &&
                        relatedIncidentIds.Contains(m.AppliedIncidentId.Value))
                    .ToList();

                var usageIds = usageMovements.Select(m => m.Id).ToList();
                var cancelledIds = _overtimeMovementLogRepository
                    .GetByFilter(m => m.MovementType == OvertimeMovementType.Cancellation &&
                        m.RelatedMovementId != null &&
                        usageIds.Contains(m.RelatedMovementId.Value))
                    .Select(m => m.RelatedMovementId!.Value)
                    .ToHashSet();

                foreach (var movement in usageMovements)
                {
                    if (cancelledIds.Contains(movement.Id))
                    {
                        continue; // Reintegrado: ya no cuenta como consumo vigente.
                    }

                    var minutes = Math.Abs(movement.Minutes);
                    overtimeByDate[movement.SourceDate] = overtimeByDate.GetValueOrDefault(movement.SourceDate) + minutes;
                }
            }

            // Construir el detalle día por día sobre el rango del permiso.
            var days = new List<AbsenceRequestDayDetail>();
            for (var date = item.StartDate; date <= item.EndDate; date = date.AddDays(1))
            {
                var minutes = overtimeByDate.GetValueOrDefault(date);
                days.Add(new AbsenceRequestDayDetail
                {
                    Date = date,
                    OvertimeMinutes = minutes,
                    OvertimeFormatted = FormatMinutes(minutes),
                });
            }

            var totalOvertime = overtimeByDate.Values.Sum();

            return new EmployeeAbsenceRequestDetailOutput
            {
                Id = item.Id,
                EmployeeName = $"{employee?.Name ?? string.Empty} {employee?.LastName ?? string.Empty} {employee?.MLastName ?? string.Empty}".Trim(),
                EmployeeCode = item.EmployeeCode,
                EmployeeActivity = key?.Tabulator.Activity ?? string.Empty,
                IncidentCode = item.IncidentCode,
                IncidentDescription = item.IncidentCodeItem?.Label ?? string.Empty,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Notes = item.Notes,
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                RequiresApproval = totalApprovers > 0,
                TotalApprovers = totalApprovers,
                ApprovedCount = approvedCount,
                DaysCount = days.Count,
                UsedOvertime = totalOvertime > 0,
                TotalOvertimeMinutes = totalOvertime,
                TotalOvertimeFormatted = FormatMinutes(totalOvertime),
                Days = days,
                ApprovalChain = approvalChain,
            };
        }

        private static string FormatMinutes(int minutes)
        {
            var safe = Math.Max(0, minutes);
            return $"{safe / 60} hrs {(safe % 60):00} min";
        }

        public bool ExecuteProcess(RegisterDaysOff registerDaysOff)
        {
            var firstDate = registerDaysOff.Dates.Min();
            var lastDate = registerDaysOff.Dates.Max();

            var company = _companyRepository.GetByFilter(c => c.Id == registerDaysOff.CompanyId).FirstOrDefault();

            if (company == null)
            {
                throw new BadHttpRequestException("La empresa no existe");
            }

            var item = new EmployeeAbsenceRequests()
            {
                CompanyId = company.Id,
                EmployeeCode = (int)registerDaysOff.EmployeeCode,
                EndDate = lastDate,
                IncidentCode = registerDaysOff.IncidentCode,
                StartDate = firstDate,
                Notes = registerDaysOff.Notes,
                Status = AbsenceRequestStatus.Pending,
            };

            _repository.Create(item);
            _repository.Save();

            // Congela la cadena de firmas (rol + alcance) para esta solicitud. Si el código
            // no tiene cadena configurada, no hace nada y se conserva el flujo previo.
            _approvalResolver.MaterializeForAbsenceRequest(item.Id, item.IncidentCode, (int)company.Id, item.EmployeeCode);

            return true;
        }

        // Recalcula los candidatos de los niveles aún no firmados de una solicitud (sin recrearla).
        public ReResolveResult ExecuteProcess(ReResolveChain input)
        {
            if (string.IsNullOrEmpty(input.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetById(Guid.Parse(input.Id));
            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            var changed = _approvalResolver.ReResolveForAbsenceRequest(item.Id, (int)item.CompanyId, item.EmployeeCode);

            return new ReResolveResult
            {
                Changed = changed,
                Message = changed > 0
                    ? $"Se actualizaron {changed} nivel(es) de firma."
                    : "No hubo cambios en la cadena de firmas.",
            };
        }

        public bool ExecuteProcess(ChangeStatus changeStatus)
        {
            if (string.IsNullOrEmpty(changeStatus.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetById(Guid.Parse(changeStatus.Id));
            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            // Si la solicitud tiene cadena de firmas congelada, se usa el flujo secuencial
            // por nivel (rol + alcance). Si no, se conserva el flujo previo (incident_approver).
            var chain = _absenceRequestApprovalRepository
                .GetByFilter(a => a.AbsenceRequestId == item.Id)
                .OrderBy(a => a.StepOrder)
                .ToList();

            if (chain.Count > 0)
            {
                return ProcessChainedApproval(item, chain, changeStatus);
            }

            // Approvers configured for the request's incidence code.
            var approvers = _incidentApproverRepository.GetByFilter(ia => ia.IncidentCode == item.IncidentCode).ToList();
            var totalApprovers = approvers.Count;

            var relatedIncidents = GetRelatedIncidents(item);
            var now = DateTime.UtcNow;
            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;

            if (changeStatus.Status == AbsenceRequestStatus.Rejected)
            {
                // Any configured approver can reject the whole request.
                if (totalApprovers > 0)
                {
                    EnsureIsApprover(approvers, currentUserId);
                }

                item.Status = AbsenceRequestStatus.Rejected;
                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();

                PropagateToIncidents(relatedIncidents, false, now);
                return true;
            }

            if (changeStatus.Status == AbsenceRequestStatus.Approved)
            {
                // No approvers configured: direct approval (legacy behavior).
                if (totalApprovers == 0 || relatedIncidents.Count == 0)
                {
                    item.Status = AbsenceRequestStatus.Approved;
                    item.UpdatedAt = now;
                    _repository.Update(item);
                    _repository.Save();

                    PropagateToIncidents(relatedIncidents, true, now);
                    return true;
                }

                // With approvers: the current user must be an approver and their approval is registered.
                var myApprover = EnsureIsApprover(approvers, currentUserId);

                foreach (var incident in relatedIncidents)
                {
                    var alreadyApproved = _assistanceIncidentApproverRepository
                        .GetByFilter(a => a.AssistanceIncidentId == incident.Id && a.IncidentApproverId == myApprover.Id)
                        .Any();

                    if (!alreadyApproved)
                    {
                        _assistanceIncidentApproverRepository.Create(new AssistanceIncidentApprover
                        {
                            AssistanceIncidentId = incident.Id,
                            IncidentApproverId = myApprover.Id,
                            ApprovalDate = now,
                            AssistanceIncident = incident,
                            IncidentApprover = myApprover,
                        });
                    }
                }
                _assistanceIncidentApproverRepository.Save();

                // The request is approved only when ALL configured approvers have approved.
                var approverIds = approvers.Select(a => a.Id).ToHashSet();
                var incidentIds = relatedIncidents.Select(i => i.Id).ToHashSet();
                var approvedCount = _assistanceIncidentApproverRepository
                    .GetByFilter(a => incidentIds.Contains(a.AssistanceIncidentId) && approverIds.Contains(a.IncidentApproverId))
                    .Select(a => a.IncidentApproverId)
                    .Distinct()
                    .Count();

                if (approvedCount >= totalApprovers)
                {
                    item.Status = AbsenceRequestStatus.Approved;
                    item.UpdatedAt = now;
                    _repository.Update(item);
                    _repository.Save();

                    PropagateToIncidents(relatedIncidents, true, now);
                }
                else
                {
                    // Partial approval: the request stays pending until the others approve.
                    item.UpdatedAt = now;
                    _repository.Update(item);
                    _repository.Save();
                }

                return true;
            }

            // Other statuses (e.g. back to Pending).
            item.Status = changeStatus.Status;
            item.UpdatedAt = now;
            _repository.Update(item);
            _repository.Save();
            return true;
        }

        // Flujo de aprobación secuencial por nivel (cadena de firmas congelada).
        // El usuario solo puede firmar/rechazar el nivel pendiente más temprano del que es
        // candidato; al aprobarse el último nivel se aprueba la solicitud y se propaga a las
        // incidencias. Un rechazo en cualquier nivel rechaza toda la solicitud.
        private bool ProcessChainedApproval(EmployeeAbsenceRequests item, List<AbsenceRequestApproval> chain, ChangeStatus changeStatus)
        {
            var now = DateTime.UtcNow;
            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;

            if (currentUserId == null)
            {
                throw new BadHttpRequestException("No se pudo identificar al usuario.");
            }

            // Estados que solo cambian la solicitud (sin firma): regresar a pendiente, etc.
            if (changeStatus.Status != AbsenceRequestStatus.Approved && changeStatus.Status != AbsenceRequestStatus.Rejected)
            {
                item.Status = changeStatus.Status;
                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();
                return true;
            }

            // Primer nivel sin resolver (los Skipped/Approved ya están resueltos y se ignoran).
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

            // El usuario debe ser candidato del nivel actual, ya sea directamente o como
            // suplente (delegación vigente) de algún candidato.
            var candidates = ParseCandidateIds(currentLevel.ResolvedCandidateUserIds);
            var delegators = GetDelegatorIdsFor(currentUserId.Value);
            var signableFor = candidates
                .Where(c => c == currentUserId.Value || delegators.Contains(c))
                .ToHashSet();

            if (signableFor.Count == 0)
            {
                throw new BadHttpRequestException("No tienes autorización para firmar este nivel de la solicitud.");
            }

            var relatedIncidents = GetRelatedIncidents(item);

            if (changeStatus.Status == AbsenceRequestStatus.Rejected)
            {
                currentLevel.Status = ApprovalInstanceStatus.Rejected;
                currentLevel.ApprovedByUserId = currentUserId;
                currentLevel.ApprovedAt = now;
                currentLevel.Comment = changeStatus.Comment;
                currentLevel.UpdatedAt = now;
                _absenceRequestApprovalRepository.Update(currentLevel);
                _absenceRequestApprovalRepository.Save();

                item.Status = AbsenceRequestStatus.Rejected;
                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();

                PropagateToIncidents(relatedIncidents, false, now);
                return true;
            }

            // Aprobación del nivel actual. Se registran los candidatos cubiertos por esta firma
            // (el propio usuario y/o los titulares a los que suple).
            var signed = ParseCandidateIds(currentLevel.SignedUserIds);
            foreach (var c in signableFor)
            {
                signed.Add(c);
            }
            currentLevel.SignedUserIds = string.Join(",", signed);

            // Modo All: el nivel se aprueba sólo cuando TODOS los candidatos han firmado.
            // Modo AnyOne: basta una firma.
            var levelCompleted = currentLevel.Mode == ApprovalStepMode.All
                ? candidates.All(c => signed.Contains(c))
                : true;

            if (!levelCompleted)
            {
                // Firma parcial dentro del nivel (modo All): la solicitud sigue pendiente.
                currentLevel.Comment = changeStatus.Comment;
                currentLevel.UpdatedAt = now;
                _absenceRequestApprovalRepository.Update(currentLevel);
                _absenceRequestApprovalRepository.Save();

                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();
                return true;
            }

            currentLevel.Status = ApprovalInstanceStatus.Approved;
            currentLevel.ApprovedByUserId = currentUserId;
            currentLevel.ApprovedAt = now;
            currentLevel.Comment = changeStatus.Comment;
            currentLevel.UpdatedAt = now;
            _absenceRequestApprovalRepository.Update(currentLevel);
            _absenceRequestApprovalRepository.Save();

            // ¿Quedan niveles por resolver después de éste? (pendientes o bloqueados).
            // Un nivel bloqueado posterior impide aprobar la solicitud hasta resolverse.
            var hasMorePending = chain.Any(l => l.Id != currentLevel.Id &&
                (l.Status == ApprovalInstanceStatus.Pending || l.Status == ApprovalInstanceStatus.Blocked));

            if (!hasMorePending)
            {
                // Última firma: la solicitud queda aprobada y se refleja en las incidencias.
                item.Status = AbsenceRequestStatus.Approved;
                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();

                PropagateToIncidents(relatedIncidents, true, now);
            }
            else
            {
                // Aprobación parcial: la solicitud sigue pendiente del resto de niveles.
                item.UpdatedAt = now;
                _repository.Update(item);
                _repository.Save();
            }

            return true;
        }

        // Titulares a los que el usuario indicado puede suplir hoy (delegación vigente).
        private HashSet<Guid> GetDelegatorIdsFor(Guid delegateUserId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return _approverDelegationRepository
                .GetByFilter(d => d.DelegateUserId == delegateUserId
                    && d.FromDate <= today
                    && (d.ToDate == null || d.ToDate >= today))
                .Select(d => d.UserId)
                .ToHashSet();
        }

        // Convierte el snapshot CSV de candidatos a un conjunto de GUIDs.
        private static HashSet<Guid> ParseCandidateIds(string? csv)
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

        // Incidences related to a request by company, employee, code and date range.
        private List<AssistanceIncident> GetRelatedIncidents(EmployeeAbsenceRequests item)
        {
            return _assistanceIncidentRepository.GetByFilter(ai =>
                ai.CompanyId == item.CompanyId &&
                ai.EmployeeCode == item.EmployeeCode &&
                ai.IncidentCode == item.IncidentCode &&
                ai.Date >= item.StartDate &&
                ai.Date <= item.EndDate
            ).ToList();
        }

        // Verifies the current user is an approver of the code; returns their approver record.
        private IncidentApprover EnsureIsApprover(List<IncidentApprover> approvers, Guid? currentUserId)
        {
            var myApprover = currentUserId != null ? approvers.FirstOrDefault(a => a.UserId == currentUserId) : null;

            if (myApprover == null)
            {
                throw new BadHttpRequestException("No tienes autorización para aprobar o rechazar esta solicitud.");
            }

            return myApprover;
        }

        // Propagates the result to the related incidences so the permit is only reflected
        // in payroll once approved.
        private void PropagateToIncidents(List<AssistanceIncident> relatedIncidents, bool approved, DateTime now)
        {
            if (relatedIncidents.Count == 0)
            {
                return;
            }

            foreach (var incident in relatedIncidents)
            {
                incident.Approved = approved;
                incident.Rejected = !approved;
                incident.UpdatedAt = now;

                if (!approved)
                {
                    incident.RejectedAt = now;
                }
                else
                {
                    incident.RejectedAt = null;
                    incident.RejectionComment = null;
                    incident.RejectedByUserId = null;
                }

                _assistanceIncidentRepository.Update(incident);
            }

            _assistanceIncidentRepository.Save();
        }

        // Renderiza el PDF del permiso usando el documento/contrato asignado: reemplaza los
        // placeholders con datos reales y construye {{signatures}} a partir de la cadena de firmas.
        private byte[] RenderPermitFromDocument(
            Document document,
            EmployeeAbsenceRequests item,
            string companyName,
            string employeeName,
            string department,
            string activity,
            int days,
            DateOnly returnDate,
            string? logoDataUrl)
        {
            // Cadena de firmas materializada de esta solicitud (ordenada).
            var chain = _absenceRequestApprovalRepository
                .GetByFilter(a => a.AbsenceRequestId == item.Id)
                .OrderBy(a => a.StepOrder)
                .ToList();

            var roleIds = chain.Select(c => c.RoleId).Distinct().ToList();
            var roleLabels = roleIds.Count == 0
                ? new Dictionary<Guid, string>()
                : _roleRepository.GetByFilter(r => roleIds.Contains(r.Id)).ToDictionary(r => r.Id, r => r.Label);

            var approverIds = chain.Where(c => c.ApprovedByUserId != null).Select(c => c.ApprovedByUserId!.Value).Distinct().ToList();
            var approverNames = approverIds.Count == 0
                ? new Dictionary<Guid, string>()
                : _userRepository.GetByFilter(u => approverIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Name);

            var blocks = chain
                .Where(c => c.Status != ApprovalInstanceStatus.Skipped)
                .Select(c => new DocumentPdfRenderer.SignatureBlock
                {
                    RoleLabel = roleLabels.TryGetValue(c.RoleId, out var label) ? label : "Firma",
                    SignedByName = c.Status == ApprovalInstanceStatus.Approved && c.ApprovedByUserId != null && approverNames.TryGetValue(c.ApprovedByUserId.Value, out var name) ? name : null,
                    SignedAt = c.ApprovedAt?.ToString("dd/MM/yyyy"),
                })
                .ToList();

            var signaturesHtml = DocumentPdfRenderer.BuildSignaturesHtml(employeeName, blocks);

            // Logo opcional (data URL base64 de Apariencia) como <img>.
            var logoHtml = string.IsNullOrWhiteSpace(logoDataUrl)
                ? string.Empty
                : $"<img src=\"{logoDataUrl}\" style=\"max-height:60px; max-width:180px;\" />";

            var values = new Dictionary<string, string>
            {
                ["logo"] = logoHtml,
                ["companyName"] = companyName,
                ["today"] = item.CreatedAt.ToString("dd/MM/yyyy"),
                ["employeeName"] = employeeName,
                ["employeeActivity"] = activity,
                ["employeeCode"] = item.EmployeeCode.ToString(),
                ["departmentName"] = department,
                ["permissionLabel"] = item.IncidentCodeItem?.Label ?? string.Empty,
                ["totalDays"] = days.ToString(),
                ["startDate"] = item.StartDate.ToString("dd/MM/yyyy"),
                ["endDate"] = item.EndDate.ToString("dd/MM/yyyy"),
                ["returnDate"] = returnDate.ToString("dd/MM/yyyy"),
                ["notes"] = item.Notes ?? string.Empty,
                ["signatures"] = signaturesHtml,
            };

            return _documentPdfRenderer.Render(document.Content!, values);
        }

        public AbsenceRequestPdf ExecuteProcess(DownloadRequest downloadRequest)
        {
            if (string.IsNullOrEmpty(downloadRequest.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetContextEntity()
                .Include(e => e.IncidentCodeItem)
                .FirstOrDefault(e => e.Id == Guid.Parse(downloadRequest.Id));

            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            if (item.Status != AbsenceRequestStatus.Approved)
            {
                throw new BadHttpRequestException("La solicitud a un no ha sido aprobada");
            }

            var keyEmployee = _keyRepository.GetContextEntity().Include(k => k.Tabulator).Include(k => k.CenterItem).Include(k => k.SupervisorItem).Include(k => k.Employee);
            // Usamos GetByFilter en lugar de GetById porque _companyRepository.GetById usa Find()
            // y EF Core falla con "Parameter value 'X' is out of range" al convertir el decimal CompanyId
            // hacia el tipo SQL del PK.
            var company = _companyRepository.GetByFilter(c => c.Id == item.CompanyId).FirstOrDefault();

            if (company == null)
            {
                throw new BadHttpRequestException("La empresa no existe");
            }

            var keys = keyEmployee.Where(k => k.Company == company.Id && (int)k.Codigo == item.EmployeeCode).SingleOrDefault();

            var days = (item.EndDate.ToDateTime(TimeOnly.MinValue) - item.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            var returnDate = item.EndDate.AddDays(1);
            var employeeFullName = $"{keys?.Employee.Name ?? string.Empty} {keys?.Employee.LastName ?? string.Empty} {keys?.Employee.MLastName ?? string.Empty}".Trim();

            // Cargar logo configurado en Apariencia (si existe) para incluirlo en el PDF.
            var appearance = _sysConfigService.ExecuteProcess<GetAppearance, SysAppearance>(new GetAppearance());
            var logo = appearance?.Logo;

            var department = _globalPropertyService.TypeTenant == TypeTenant.Department
                ? keys?.CenterItem?.DepartmentName ?? string.Empty
                : keys?.SupervisorItem?.Name ?? string.Empty;

            byte[] bytes;

            // Si el código tiene un documento/contrato asignado, se renderiza ese formato y las
            // firmas se construyen desde la cadena de firmantes. Si no, se usa el formato fijo.
            var document = item.IncidentCodeItem?.DocumentId != null
                ? _documentRepository.GetById(item.IncidentCodeItem.DocumentId.Value)
                : null;

            if (document != null && !string.IsNullOrWhiteSpace(document.Content))
            {
                bytes = RenderPermitFromDocument(document, item, company.Name, employeeFullName, department, $"{keys?.Tabulator.Activity}", days, returnDate, logo);
            }
            else
            {
                bytes = _permissionPdfService.Generate(
                    company.Name,
                    employeeFullName,
                    $"{item.EmployeeCode}",
                    $"{keys?.Tabulator.Activity}",
                    department,
                    item.CreatedAt.ToString("dd/MM/yyyy"),
                    item.IncidentCodeItem?.Label ?? string.Empty,
                    item.Notes ?? string.Empty,
                    item.StartDate.ToString("dd/MM/yyyy"),
                    item.EndDate.ToString("dd/MM/yyyy"),
                    returnDate.ToString("dd/MM/yyyy"),
                    days.ToString(),
                    logo
                );
            }

            // Sanitize filename: replace spaces and slashes
            var safeName = string.IsNullOrWhiteSpace(employeeFullName) ? $"Empleado_{item.EmployeeCode}" : employeeFullName.Replace(' ', '_');
            var safeDate = item.StartDate.ToString("yyyy-MM-dd");
            var fileName = $"Solicitud_{safeName}_{safeDate}.pdf";

            return new AbsenceRequestPdf { Bytes = bytes, FileName = fileName };
        }
    }
}
