using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest;
using PrenominaApi.Models.Dto.Output.OvertimePayment;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Utilities.PermissionPdf;

namespace PrenominaApi.Services.Prenomina
{
    /// <summary>
    /// Aprobación/rechazo de las papeletas de pago de horas extras. Reutiliza la progresión
    /// de cadena (ApprovalFlowService). Al rechazar, reintegra las horas: cancela los movimientos
    /// de pago directo vinculados, de modo que los días vuelven a "pendientes".
    /// </summary>
    public class OvertimePaymentRequestService
    {
        private readonly PrenominaDbContext _context;
        private readonly ApprovalFlowService _approvalFlow;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly ApprovalResolver _approvalResolver;
        private readonly IBaseRepository<Company> _companyRepository;
        private readonly IBaseServicePrenomina<SystemConfig> _sysConfigService;
        private readonly DocumentPdfRenderer _documentPdfRenderer;

        public OvertimePaymentRequestService(
            PrenominaDbContext context,
            ApprovalFlowService approvalFlow,
            GlobalPropertyService globalPropertyService,
            IBaseRepository<Key> keyRepository,
            ApprovalResolver approvalResolver,
            IBaseRepository<Company> companyRepository,
            IBaseServicePrenomina<SystemConfig> sysConfigService,
            DocumentPdfRenderer documentPdfRenderer)
        {
            _context = context;
            _approvalFlow = approvalFlow;
            _globalPropertyService = globalPropertyService;
            _keyRepository = keyRepository;
            _approvalResolver = approvalResolver;
            _companyRepository = companyRepository;
            _sysConfigService = sysConfigService;
            _documentPdfRenderer = documentPdfRenderer;
        }

        /// <summary>
        /// Genera el PDF de la papeleta de pago a partir del documento/contrato asignado:
        /// reemplaza placeholders con datos del empleado y construye {{signatures}} desde la cadena.
        /// </summary>
        public async Task<AbsenceRequestPdf> GeneratePdf(Guid id)
        {
            var request = await _context.overtimePaymentRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new BadHttpRequestException("La solicitud de pago no existe.");
            }

            if (request.DocumentId == null)
            {
                throw new BadHttpRequestException("La papeleta no tiene un formato de horas extras asignado.");
            }

            var document = await _context.documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DocumentId.Value);
            if (document == null || string.IsNullOrWhiteSpace(document.Content))
            {
                throw new BadHttpRequestException("El formato de horas extras no tiene contenido.");
            }

            var key = await _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Include(k => k.Tabulator)
                .Include(k => k.Employee)
                .Include(k => k.CenterItem)
                .FirstOrDefaultAsync(k => k.Company == request.CompanyId && (int)k.Codigo == request.EmployeeCode);

            var employeeName = $"{key?.Employee?.Name ?? string.Empty} {key?.Employee?.LastName ?? string.Empty} {key?.Employee?.MLastName ?? string.Empty}".Trim();
            var department = key?.CenterItem?.DepartmentName ?? string.Empty;
            var activity = key?.Tabulator?.Activity ?? string.Empty;

            var company = _companyRepository.GetByFilter(c => c.Id == request.CompanyId).FirstOrDefault();

            var appearance = _sysConfigService.ExecuteProcess<GetAppearance, SysAppearance>(new GetAppearance());
            var logo = appearance?.Logo;
            var logoHtml = string.IsNullOrWhiteSpace(logo)
                ? string.Empty
                : $"<img src=\"{logo}\" style=\"max-height:60px; max-width:180px;\" />";

            // Firmas desde la cadena materializada.
            var chain = await _context.absenceRequestApprovals
                .AsNoTracking()
                .Where(a => a.RequestType == ApprovalRequestType.OvertimePayment && a.AbsenceRequestId == request.Id)
                .OrderBy(a => a.StepOrder)
                .ToListAsync();

            var blocks = new List<DocumentPdfRenderer.SignatureBlock>();
            if (chain.Count > 0)
            {
                var roleIds = chain.Select(c => c.RoleId).Distinct().ToList();
                var roleLabels = await _context.roles.AsNoTracking()
                    .Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Label);
                var approverIds = chain.Where(c => c.ApprovedByUserId != null).Select(c => c.ApprovedByUserId!.Value).Distinct().ToList();
                var approverNames = approverIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _context.users.AsNoTracking().Where(u => approverIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name);

                blocks = chain
                    .Where(c => c.Status != ApprovalInstanceStatus.Skipped)
                    .Select(c => new DocumentPdfRenderer.SignatureBlock
                    {
                        RoleLabel = roleLabels.TryGetValue(c.RoleId, out var label) ? label : "Firma",
                        SignedByName = c.Status == ApprovalInstanceStatus.Approved && c.ApprovedByUserId != null && approverNames.TryGetValue(c.ApprovedByUserId.Value, out var name) ? name : null,
                        SignedAt = c.ApprovedAt?.ToString("dd/MM/yyyy"),
                    })
                    .ToList();
            }

            var signaturesHtml = DocumentPdfRenderer.BuildSignaturesHtml(employeeName, blocks);

            // Fechas de donde se tomaron las horas extras: fechas origen de los movimientos de
            // pago directo vinculados a la papeleta, distintas y ordenadas, separadas por coma.
            var overtimeDates = await _context.overtimeMovementLogs
                .AsNoTracking()
                .Where(m => m.OvertimePaymentRequestId == request.Id
                    && m.MovementType == OvertimeMovementType.DirectPayment)
                .Select(m => m.SourceDate)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
            var overtimeDatesText = string.Join(", ", overtimeDates.Select(d => d.ToString("dd/MM/yyyy")));

            var values = new Dictionary<string, string>
            {
                ["logo"] = logoHtml,
                ["companyName"] = company?.Name ?? string.Empty,
                ["employeeName"] = employeeName,
                ["employeeActivity"] = activity,
                ["employeeCode"] = request.EmployeeCode.ToString(),
                ["departmentName"] = department,
                ["totalOvertime"] = FormatMinutes(request.TotalMinutes),
                ["totalMinutes"] = request.TotalMinutes.ToString(),
                ["overtimeDates"] = overtimeDatesText,
                ["today"] = request.CreatedAt.ToString("dd/MM/yyyy"),
                ["notes"] = request.Notes ?? string.Empty,
                ["signatures"] = signaturesHtml,
            };

            var bytes = _documentPdfRenderer.Render(document.Content!, values);
            var safeName = string.IsNullOrWhiteSpace(employeeName) ? $"Empleado_{request.EmployeeCode}" : employeeName.Replace(' ', '_');
            var fileName = $"PagoHorasExtra_{safeName}_{request.CreatedAt:yyyy-MM-dd}.pdf";

            return new AbsenceRequestPdf { Bytes = bytes, FileName = fileName };
        }

        public async Task<int> ReResolve(Guid id)
        {
            var request = await _context.overtimePaymentRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new BadHttpRequestException("La solicitud de pago no existe.");
            }

            return _approvalResolver.ReResolveForAbsenceRequest(request.Id, request.CompanyId, request.EmployeeCode);
        }

        private static string FormatMinutes(int minutes)
        {
            var safe = Math.Max(0, minutes);
            return $"{safe / 60} hrs {(safe % 60):00} min";
        }

        public async Task<List<OvertimePaymentRequestOutput>> GetList(int companyId)
        {
            var requests = await _context.overtimePaymentRequests
                .AsNoTracking()
                .Where(r => r.CompanyId == companyId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (requests.Count == 0)
            {
                return new List<OvertimePaymentRequestOutput>();
            }

            // Filtrar por el centro/supervisor seleccionado (a menos que sea "TODOS" = -999).
            var tenant = _globalPropertyService.Tenant;
            if (!string.IsNullOrEmpty(tenant) && tenant != "-999" && tenant != "all")
            {
                var codes = requests.Select(r => r.EmployeeCode).Distinct().ToList();
                var keysQuery = _keyRepository.GetContextEntity().AsNoTracking()
                    .Where(k => k.Company == companyId && codes.Contains((int)k.Codigo));

                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    keysQuery = keysQuery.Where(k => k.Center == tenant);
                }
                else
                {
                    var supervisorId = Convert.ToDecimal(tenant);
                    keysQuery = keysQuery.Where(k => k.Supervisor == supervisorId);
                }

                var allowedCodes = (await keysQuery.Select(k => (int)k.Codigo).ToListAsync()).ToHashSet();
                requests = requests.Where(r => allowedCodes.Contains(r.EmployeeCode)).ToList();

                if (requests.Count == 0)
                {
                    return new List<OvertimePaymentRequestOutput>();
                }
            }

            var requestIds = requests.Select(r => r.Id).ToList();
            var chainsByRequest = (await _context.absenceRequestApprovals
                    .AsNoTracking()
                    .Where(a => a.RequestType == ApprovalRequestType.OvertimePayment && requestIds.Contains(a.AbsenceRequestId))
                    .ToListAsync())
                .GroupBy(a => a.AbsenceRequestId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StepOrder).ToList());

            var names = await GetEmployeeNames(requests.Select(r => r.EmployeeCode).Distinct().ToList(), companyId);

            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;
            var delegators = currentUserId != null ? _approvalFlow.GetDelegatorIdsFor(currentUserId.Value) : new HashSet<Guid>();

            return requests.Select(r =>
            {
                chainsByRequest.TryGetValue(r.Id, out var chain);
                chain ??= new List<AbsenceRequestApproval>();
                var effective = chain.Where(l => l.Status != ApprovalInstanceStatus.Skipped).ToList();
                var current = chain.FirstOrDefault(l => l.Status == ApprovalInstanceStatus.Pending);
                var currentCandidates = current != null ? ApprovalFlowService.ParseCandidateIds(current.ResolvedCandidateUserIds) : new HashSet<Guid>();
                var currentSigned = current != null ? ApprovalFlowService.ParseCandidateIds(current.SignedUserIds) : new HashSet<Guid>();
                var canApprove = current != null && currentUserId != null
                    && currentCandidates.Any(c => (c == currentUserId.Value || delegators.Contains(c)) && !currentSigned.Contains(c));
                var iApproved = currentUserId != null && chain.Any(l => l.Status == ApprovalInstanceStatus.Approved && l.ApprovedByUserId == currentUserId);

                return new OvertimePaymentRequestOutput
                {
                    Id = r.Id,
                    EmployeeName = names.TryGetValue(r.EmployeeCode, out var n) ? n : string.Empty,
                    EmployeeCode = r.EmployeeCode,
                    TotalMinutes = r.TotalMinutes,
                    TotalMinutesFormatted = FormatMinutes(r.TotalMinutes),
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    Notes = r.Notes,
                    RequiresApproval = chain.Count > 0,
                    TotalApprovers = effective.Count,
                    ApprovedCount = effective.Count(l => l.Status == ApprovalInstanceStatus.Approved),
                    AlreadyApprovedByMe = iApproved && !canApprove,
                    CanApprove = canApprove,
                };
            }).ToList();
        }

        public async Task<OvertimePaymentRequestDetailOutput> GetDetail(Guid id)
        {
            var request = await _context.overtimePaymentRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new BadHttpRequestException("La solicitud de pago no existe.");
            }

            var names = await GetEmployeeNames(new List<int> { request.EmployeeCode }, request.CompanyId);

            var chain = await _context.absenceRequestApprovals
                .AsNoTracking()
                .Where(a => a.RequestType == ApprovalRequestType.OvertimePayment && a.AbsenceRequestId == request.Id)
                .OrderBy(a => a.StepOrder)
                .ToListAsync();

            var approvalChain = new List<AbsenceRequestApprovalStepOutput>();
            if (chain.Count > 0)
            {
                var roleIds = chain.Select(c => c.RoleId).Distinct().ToList();
                var roleLabels = await _context.roles.AsNoTracking()
                    .Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Label);

                var candidatesByLevel = chain.ToDictionary(c => c.Id, c => ApprovalFlowService.ParseCandidateIds(c.ResolvedCandidateUserIds).ToList());
                var userIds = chain.Where(c => c.ApprovedByUserId != null).Select(c => c.ApprovedByUserId!.Value)
                    .Concat(candidatesByLevel.Values.SelectMany(v => v)).Distinct().ToList();
                var userNames = userIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _context.users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name);

                var current = chain.FirstOrDefault(l => l.Status == ApprovalInstanceStatus.Pending);

                approvalChain = chain.Select(c => new AbsenceRequestApprovalStepOutput
                {
                    StepOrder = c.StepOrder,
                    RoleLabel = roleLabels.TryGetValue(c.RoleId, out var label) ? label : "Rol",
                    Scope = c.Scope.ToString(),
                    Status = c.Status.ToString(),
                    IsCurrent = current != null && c.Id == current.Id,
                    ApprovedByName = c.ApprovedByUserId != null && userNames.TryGetValue(c.ApprovedByUserId.Value, out var an) ? an : null,
                    ApprovedAt = c.ApprovedAt,
                    Comment = c.Comment,
                    CandidateNames = candidatesByLevel.TryGetValue(c.Id, out var cands)
                        ? cands.Select(uid2 => userNames.TryGetValue(uid2, out var cn) ? cn : null).Where(x => x != null).Cast<string>().ToList()
                        : new List<string>(),
                }).ToList();
            }

            return new OvertimePaymentRequestDetailOutput
            {
                Id = request.Id,
                EmployeeName = names.TryGetValue(request.EmployeeCode, out var name) ? name : string.Empty,
                EmployeeCode = request.EmployeeCode,
                TotalMinutes = request.TotalMinutes,
                TotalMinutesFormatted = FormatMinutes(request.TotalMinutes),
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                Notes = request.Notes,
                ApprovalChain = approvalChain,
            };
        }

        private async Task<Dictionary<int, string>> GetEmployeeNames(List<int> employeeCodes, int companyId)
        {
            if (employeeCodes.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var keys = await _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Include(k => k.Employee)
                .Where(k => k.Company == companyId && employeeCodes.Contains((int)k.Codigo))
                .ToListAsync();

            return keys
                .GroupBy(k => (int)k.Codigo)
                .ToDictionary(
                    g => g.Key,
                    g => $"{g.First().Employee?.Name ?? string.Empty} {g.First().Employee?.LastName ?? string.Empty} {g.First().Employee?.MLastName ?? string.Empty}".Trim());
        }

        public async Task<bool> ChangeStatus(Guid id, bool approve, string? comment)
        {
            var request = await _context.overtimePaymentRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new BadHttpRequestException("La solicitud de pago no existe.");
            }

            var outcome = _approvalFlow.Advance(id, approve, comment);
            var now = DateTime.UtcNow;

            if (outcome == ApprovalOutcome.Rejected)
            {
                request.Status = AbsenceRequestStatus.Rejected;
                await ReintegrateHours(request, comment);
            }
            else if (outcome == ApprovalOutcome.Approved)
            {
                request.Status = AbsenceRequestStatus.Approved;
            }
            // ApprovalOutcome.Partial: la solicitud sigue pendiente.

            request.UpdatedAt = now;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Reintegra las horas pagadas de una papeleta rechazada: por cada movimiento de pago
        /// directo no cancelado, crea una cancelación y revierte los minutos pagados. El resumen
        /// excluye los movimientos cancelados, así que los días vuelven a "pendientes".
        /// </summary>
        private async Task ReintegrateHours(OvertimePaymentRequest request, string? reason)
        {
            var byUser = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : Guid.Empty;
            var now = DateTime.UtcNow;

            var movements = await _context.overtimeMovementLogs
                .Include(m => m.OvertimeAccumulation)
                .Where(m => m.OvertimePaymentRequestId == request.Id
                    && m.MovementType == OvertimeMovementType.DirectPayment)
                .ToListAsync();

            if (movements.Count == 0)
            {
                return;
            }

            var movementIds = movements.Select(m => m.Id).ToList();
            var alreadyCancelled = (await _context.overtimeMovementLogs
                .Where(m => m.MovementType == OvertimeMovementType.Cancellation
                    && m.RelatedMovementId != null
                    && movementIds.Contains(m.RelatedMovementId.Value))
                .Select(m => m.RelatedMovementId!.Value)
                .ToListAsync())
                .ToHashSet();

            foreach (var movement in movements)
            {
                if (alreadyCancelled.Contains(movement.Id))
                {
                    continue;
                }

                var accumulation = movement.OvertimeAccumulation;
                if (accumulation == null)
                {
                    continue;
                }

                accumulation.PaidMinutes -= movement.Minutes; // revertir lo pagado
                accumulation.UpdatedAt = now;

                _context.overtimeMovementLogs.Add(new OvertimeMovementLog
                {
                    OvertimeAccumulationId = accumulation.Id,
                    EmployeeCode = movement.EmployeeCode,
                    CompanyId = movement.CompanyId,
                    MovementType = OvertimeMovementType.Cancellation,
                    Minutes = -movement.Minutes,
                    BalanceAfter = accumulation.AccumulatedMinutes,
                    SourceDate = movement.SourceDate,
                    OvertimePaymentRequestId = request.Id,
                    Notes = $"Reintegro por rechazo de pago de horas extras: {reason}",
                    ByUserId = byUser,
                    RelatedMovementId = movement.Id,
                    CreatedAt = now,
                });
            }
        }
    }
}
