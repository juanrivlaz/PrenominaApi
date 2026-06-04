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

        public EmployeeAbsenceRequestsService(
            IBaseRepositoryPrenomina<EmployeeAbsenceRequests> repository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Company> companyRepository,
            GlobalPropertyService globalPropertyService,
            PermissionPdfService permissionPdfService,
            IBaseServicePrenomina<SystemConfig> sysConfigService,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncidentRepository,
            IBaseRepositoryPrenomina<IncidentApprover> incidentApproverRepository,
            IBaseRepositoryPrenomina<AssistanceIncidentApprover> assistanceIncidentApproverRepository
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

            // ===== Progreso de aprobación múltiple =====
            // Aprobadores configurados por código de incidencia presente en las solicitudes.
            var incidentCodes = requests.Select(r => r.IncidentCode).Distinct().ToList();
            var approversByCode = _incidentApproverRepository
                .GetByFilter(ia => incidentCodes.Contains(ia.IncidentCode))
                .GroupBy(ia => ia.IncidentCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Incidencias relacionadas a las solicitudes (por empresa, empleado y código) para
            // contar las aprobaciones ya registradas (se reutiliza el mecanismo de aprobadores
            // de incidencias). El emparejamiento por rango de fechas se hace en memoria.
            var relatedIncidents = _assistanceIncidentRepository
                .GetByFilter(ai => ai.CompanyId == companyId && employeeCodes.Contains(ai.EmployeeCode) && incidentCodes.Contains(ai.IncidentCode))
                .ToList();

            var relatedIncidentIds = relatedIncidents.Select(i => i.Id).ToHashSet();
            var approvalsByIncident = _assistanceIncidentApproverRepository
                .GetByFilter(a => relatedIncidentIds.Contains(a.AssistanceIncidentId))
                .GroupBy(a => a.AssistanceIncidentId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.IncidentApproverId).Distinct().ToList());

            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;

            var result = requests.Select(r =>
            {
                var key = keys.FirstOrDefault(k => k.Codigo == r.EmployeeCode);
                var employee = key?.Employee;
                var activity = key?.Tabulator.Activity;
                var incident = r.IncidentCodeItem;

                approversByCode.TryGetValue(r.IncidentCode, out var codeApprovers);
                var totalApprovers = codeApprovers?.Count ?? 0;
                var approverIds = codeApprovers?.Select(a => a.Id).ToHashSet() ?? new HashSet<Guid>();

                // Incidencias de esta solicitud (mismo empleado/código dentro del rango de fechas).
                var requestIncidentIds = relatedIncidents
                    .Where(ai => ai.EmployeeCode == r.EmployeeCode && ai.IncidentCode == r.IncidentCode && ai.Date >= r.StartDate && ai.Date <= r.EndDate)
                    .Select(ai => ai.Id)
                    .ToList();

                // Aprobadores (válidos) que ya registraron su aprobación en la solicitud.
                var approvedApproverIds = requestIncidentIds
                    .SelectMany(id => approvalsByIncident.TryGetValue(id, out var list) ? list : Enumerable.Empty<Guid>())
                    .Where(id => approverIds.Contains(id))
                    .Distinct()
                    .ToList();

                var myApprover = codeApprovers?.FirstOrDefault(a => currentUserId != null && a.UserId == currentUserId);

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
                    RequiresApproval = totalApprovers > 0,
                    TotalApprovers = totalApprovers,
                    ApprovedCount = approvedApproverIds.Count,
                    AlreadyApprovedByMe = myApprover != null && approvedApproverIds.Contains(myApprover.Id),
                    CanApprove = totalApprovers == 0 || myApprover != null,
                };
            });

            return result;
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

            return true;
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

            // Aprobadores configurados para el código de incidencia de la solicitud.
            var approvers = _incidentApproverRepository.GetByFilter(ia => ia.IncidentCode == item.IncidentCode).ToList();
            var totalApprovers = approvers.Count;

            var relatedIncidents = GetRelatedIncidents(item);
            var now = DateTime.UtcNow;
            Guid? currentUserId = Guid.TryParse(_globalPropertyService.UserId, out var uid) ? uid : null;

            if (changeStatus.Status == AbsenceRequestStatus.Rejected)
            {
                // Cualquier aprobador configurado puede rechazar la solicitud completa.
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
                // Sin aprobadores configurados: aprobación directa (comportamiento histórico).
                if (totalApprovers == 0 || relatedIncidents.Count == 0)
                {
                    item.Status = AbsenceRequestStatus.Approved;
                    item.UpdatedAt = now;
                    _repository.Update(item);
                    _repository.Save();

                    PropagateToIncidents(relatedIncidents, true, now);
                    return true;
                }

                // Con aprobadores: el usuario actual debe ser aprobador y se registra su aprobación.
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

                // La solicitud queda aprobada sólo cuando TODOS los aprobadores configurados aprobaron.
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
                    // Aprobación parcial: la solicitud sigue pendiente hasta que aprueben los demás.
                    item.UpdatedAt = now;
                    _repository.Update(item);
                    _repository.Save();
                }

                return true;
            }

            // Otros estados (p. ej. volver a Pendiente).
            item.Status = changeStatus.Status;
            item.UpdatedAt = now;
            _repository.Update(item);
            _repository.Save();
            return true;
        }

        // Incidencias relacionadas a una solicitud por empresa, empleado, código y rango de fechas.
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

        // Verifica que el usuario actual sea aprobador del código; devuelve su registro de aprobador.
        private IncidentApprover EnsureIsApprover(List<IncidentApprover> approvers, Guid? currentUserId)
        {
            var myApprover = currentUserId != null ? approvers.FirstOrDefault(a => a.UserId == currentUserId) : null;

            if (myApprover == null)
            {
                throw new BadHttpRequestException("No tienes autorización para aprobar o rechazar esta solicitud.");
            }

            return myApprover;
        }

        // Propaga el resultado a las incidencias relacionadas para que el permiso sólo se refleje
        // en la prenómina una vez aprobado.
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

            var bytes = _permissionPdfService.Generate(
                company.Name,
                employeeFullName,
                $"{item.EmployeeCode}",
                $"{keys?.Tabulator.Activity}",
                _globalPropertyService.TypeTenant == TypeTenant.Department ? keys?.CenterItem?.DepartmentName ?? string.Empty :
                keys?.SupervisorItem?.Name ?? string.Empty,
                item.CreatedAt.ToString("dd/MM/yyyy"),
                item.IncidentCodeItem?.Label ?? string.Empty,
                item.Notes ?? string.Empty,
                item.StartDate.ToString("dd/MM/yyyy"),
                item.EndDate.ToString("dd/MM/yyyy"),
                returnDate.ToString("dd/MM/yyyy"),
                days.ToString(),
                logo
            );

            // Sanitize filename: replace spaces and slashes
            var safeName = string.IsNullOrWhiteSpace(employeeFullName) ? $"Empleado_{item.EmployeeCode}" : employeeFullName.Replace(' ', '_');
            var safeDate = item.StartDate.ToString("yyyy-MM-dd");
            var fileName = $"Solicitud_{safeName}_{safeDate}.pdf";

            return new AbsenceRequestPdf { Bytes = bytes, FileName = fileName };
        }
    }
}
