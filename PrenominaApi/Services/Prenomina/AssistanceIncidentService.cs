using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Attendance;
using PrenominaApi.Models.Dto.Output.Attendance;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Prenomina.Helpers;

namespace PrenominaApi.Services.Prenomina
{
    public class AssistanceIncidentService : ServicePrenomina<AssistanceIncident>
    {
        private readonly IBaseRepositoryPrenomina<IncidentCode> _incidentCodeRepository;
        private readonly IBaseRepositoryPrenomina<AuditLog> _auditLogRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> _ignoreIncidentToEmployeeRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToActivity> _ignoreIncidentToActivityRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeCheckIns> _employeeCheckInsRepository;
        private readonly IBaseRepositoryPrenomina<User> _userRepository;
        private readonly IBaseRepositoryPrenomina<IncidentApprover> _incidentApproverRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncidentApprover> _assistanceIncidentApproverRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeAbsenceRequests> _employeeAbsenceRequestsRepository;

        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodService;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Employee> _employeeRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly EmployeeScheduleResolver _scheduleResolver;

        public AssistanceIncidentService(
            IBaseRepositoryPrenomina<AssistanceIncident> repository,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<AuditLog> auditLogRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> ignoreIncidentToEmployeeRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToActivity> ignoreIncidentToActivityRepository,
            IBaseRepositoryPrenomina<EmployeeCheckIns> employeeCheckInsRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseRepositoryPrenomina<IncidentApprover> incidentApproverRepository,
            IBaseRepositoryPrenomina<AssistanceIncidentApprover> assistanceIncidentApproverRepository,
            IBaseRepositoryPrenomina<EmployeeAbsenceRequests> employeeAbsenceRequestsRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodService,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Employee> employeeRepository,
            GlobalPropertyService globalPropertyService,
            EmployeeScheduleResolver scheduleResolver
        ) : base(repository) {
            _incidentCodeRepository = incidentCodeRepository;
            _auditLogRepository = auditLogRepository;
            _ignoreIncidentToEmployeeRepository = ignoreIncidentToEmployeeRepository;
            _ignoreIncidentToActivityRepository = ignoreIncidentToActivityRepository;
            _employeeCheckInsRepository = employeeCheckInsRepository;
            _userRepository = userRepository;
            _incidentApproverRepository = incidentApproverRepository;
            _assistanceIncidentApproverRepository = assistanceIncidentApproverRepository;
            _employeeAbsenceRequestsRepository = employeeAbsenceRequestsRepository;

            _keyRepository = keyRepository;
            _employeeRepository = employeeRepository;
            _globalPropertyService = globalPropertyService;
            _periodService = periodService;
            _scheduleResolver = scheduleResolver;
        }

        public AssistanceIncident ExecuteProcess(ApplyIncident applyIncident)
        {
            if (String.IsNullOrEmpty(_globalPropertyService.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var user = _userRepository.GetContextEntity().Include(u => u.Role).Where(u => u.Id == Guid.Parse(_globalPropertyService.UserId)).FirstOrDefault();

            if (user == null)
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var findIncidentCode = _incidentCodeRepository.GetContextEntity().Include(ic => ic.IncidentCodeAllowedRoles).Include(ic => ic.IncidentCodeMetadata).Where(ic => ic.Code == applyIncident.IncidentCode ).FirstOrDefault();

            if (findIncidentCode == null) {
                throw new BadHttpRequestException("El código de incidencia no existe");
            }

            var findAssistanceIncident = _repository.GetByFilter((item) => item.CompanyId == applyIncident.CompanyId && item.EmployeeCode == applyIncident.EmployeeCode && item.Date == applyIncident.Date).FirstOrDefault();

            if (findIncidentCode.IsAdditional && findAssistanceIncident != null && findAssistanceIncident.IncidentCode == applyIncident.IncidentCode && findAssistanceIncident.Date == applyIncident.Date)
            {
                throw new BadHttpRequestException("El código de incidencia ya está registrado para este empleado en la misma fecha.");
            }

            if (findIncidentCode.RestrictedWithRoles)
            {
                if (!findIncidentCode.IncidentCodeAllowedRoles!.Where(icr => icr.RoleId == user.RoleId).Any() && user!.Role!.Code != RoleCode.Sudo)
                {
                    throw new BadHttpRequestException("No tienes autorización para insertar esta incidencia");
                }                
            }

            bool? ignoreIncident = _ignoreIncidentToEmployeeRepository.GetByFilter(ie => ie.IncidentCode == findIncidentCode.Code && ie.EmployeeCode == applyIncident.EmployeeCode).FirstOrDefault()?.Ignore;
            bool? ignoreActivity = null;
            var key = _keyRepository.GetByFilter(k => k.Codigo == applyIncident.EmployeeCode && k.Company == applyIncident.CompanyId).FirstOrDefault();

            if (key != null)
            {
                ignoreActivity = _ignoreIncidentToActivityRepository.GetByFilter(ia => ia.IncidentCode == findIncidentCode.Code && ia.ActivityId == key.Ocupation).FirstOrDefault()?.Ignore;
            } else
            {
                throw new BadHttpRequestException("El empleado no se encuentra en la empresa seleccionada.");
            }

            var findPeriod = _periodService.ExecuteProcess<FindPeriod, Models.Prenomina.Period?>(new FindPeriod() {
                CompanyId = applyIncident.CompanyId,
                Date = applyIncident.Date,
                TypePayroll = key.TypeNom,
                Year = _globalPropertyService.YearOfOperation,
            });

            if (findPeriod == null)
            {
                throw new BadHttpRequestException("La fecha no se encuentra en ningun periodo.");
            }

            var closedPeriod = _periodService.ExecuteProcess<VerifyClosedPeriod, bool>(new VerifyClosedPeriod()
            {
                CompanyId = applyIncident.CompanyId,
                TenantId = _globalPropertyService.TypeTenant == TypeTenant.Department ? key.Center.Trim() : key.Supervisor.ToString(),
                NumPeriod = findPeriod.NumPeriod,
                TypePayroll = key.TypeNom,
                Year = _globalPropertyService.YearOfOperation,
            });

            if (closedPeriod)
            {
                throw new BadHttpRequestException("El periodo se encuentra cerrado.");
            }

            /*if (_globalPropertyService.TypeTenant == TypeTenant.Department && key != null)
            {
                ignoreTenant = _ignoreIncidentToActivityRepository.GetByFilter(it => it.IncidentCode == findIncidentCode.Code && it.ActivityId == key.Ocupation).FirstOrDefault()?.Ignore;
            } else if (_globalPropertyService.TypeTenant == TypeTenant.Supervisor && key != null)
            {
                ignoreTenant = _ignoreIncidentToActivityRepository.GetByFilter(it => it.IncidentCode == findIncidentCode.Code && it.SupervisorId == key.Supervisor).FirstOrDefault()?.Ignore;
            }*/

            if (ignoreIncident == true || (ignoreIncident != null && ignoreActivity == true))
            {
                throw new BadHttpRequestException("Incidencia excluida para este colaborador");
            }

            using var transaction = _repository.GetDbContext().Database.BeginTransaction();

            try
            {
                if (findAssistanceIncident != null && !findIncidentCode.IsAdditional)
                {
                    var prevIncidentCode = findAssistanceIncident.IncidentCode;
                    findAssistanceIncident.IncidentCode = applyIncident.IncidentCode;
                    findAssistanceIncident.UpdatedAt = DateTime.UtcNow;

                    // Al cambiar el código se reinicia el estado de aprobación: si el nuevo código
                    // requiere aprobación, la incidencia vuelve a quedar pendiente y se limpian las
                    // aprobaciones previas para que no se refleje en la prenómina hasta aprobarse.
                    if (prevIncidentCode != applyIncident.IncidentCode)
                    {
                        findAssistanceIncident.Approved = !findIncidentCode.RequiredApproval;
                        findAssistanceIncident.Rejected = false;
                        findAssistanceIncident.RejectionComment = null;
                        findAssistanceIncident.RejectedByUserId = null;
                        findAssistanceIncident.RejectedAt = null;

                        var prevApprovals = _assistanceIncidentApproverRepository
                            .GetByFilter(a => a.AssistanceIncidentId == findAssistanceIncident.Id)
                            .ToList();
                        foreach (var prevApproval in prevApprovals)
                        {
                            _assistanceIncidentApproverRepository.Delete(prevApproval);
                        }
                    }

                    _repository.Update(findAssistanceIncident);

                    if (prevIncidentCode != applyIncident.IncidentCode)
                    {
                        _auditLogRepository.Create(new AuditLog()
                        {
                            SectionCode = "TASISTENCIA",
                            RecordId = findAssistanceIncident.Id.ToString(),
                            Description = $"Se modificó la incidencia de asistencia para el empleado {findAssistanceIncident.EmployeeCode} de la empresa {findAssistanceIncident.CompanyId} el día {findAssistanceIncident.Date}.",
                            OldValue = prevIncidentCode,
                            NewValue = applyIncident.IncidentCode,
                            ByUserId = Guid.Parse(applyIncident.UserId!),
                        });
                    }
                }
                else
                {
                    MetaIncidentCode? metaIncidentCode = null;
                    if (findIncidentCode.IsAdditional && findIncidentCode.WithOperation)
                    {

                        if (applyIncident.Amount == null)
                        {
                            throw new BadHttpRequestException("El monto es requerido.");
                        }

                        if (findIncidentCode.IncidentCodeMetadata == null)
                        {
                            throw new BadHttpRequestException("La incidencia esta mal configurada.");
                        }

                        if (findIncidentCode.IncidentCodeMetadata.ColumnForOperation == ColumnForOperation.Custom && findIncidentCode.IncidentCodeMetadata.CustomValue == null)
                        {
                            throw new BadHttpRequestException("El valor de la incidencia no puede ser null o 0.");
                        }

                        decimal baseValue = 0;

                        if (findIncidentCode.IncidentCodeMetadata.ColumnForOperation == ColumnForOperation.Custom)
                        {
                            baseValue = (decimal)findIncidentCode.IncidentCodeMetadata.CustomValue!;
                        }
                        else
                        {
                            var employee = _employeeRepository.GetByFilter(e => e.Company == applyIncident.CompanyId && e.Codigo == applyIncident.EmployeeCode).SingleOrDefault();

                            if (employee == null)
                            {
                                throw new BadHttpRequestException("El empleado no existe");
                            }

                            baseValue = employee.Salary;
                        }

                        metaIncidentCode = new MetaIncidentCode()
                        {
                            BaseValue = baseValue,
                            OperationValue = (decimal)applyIncident.Amount,
                        };
                    }

                    findAssistanceIncident = _repository.Create(new AssistanceIncident()
                    {
                        CompanyId = applyIncident.CompanyId,
                        EmployeeCode = applyIncident.EmployeeCode,
                        Date = applyIncident.Date,
                        IncidentCode = applyIncident.IncidentCode,
                        TimeOffRequest = false,
                        Approved = !findIncidentCode.RequiredApproval,
                        Notes = applyIncident.Notes,
                        ByUserId = Guid.Parse(applyIncident.UserId!),
                        MetaIncidentCodeJson = metaIncidentCode == null ? null : JsonConvert.SerializeObject(metaIncidentCode)
                    });
                }

                _repository.Save();
                _auditLogRepository.Save();

                transaction.Commit();

                return findAssistanceIncident;

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine(ex);
                throw;
            }
        }

        public IEnumerable<PendingIncidenceApprovalOutput> ExecuteProcess(GetPendingIncidenceApprovals input)
        {
            if (string.IsNullOrEmpty(input.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var userId = Guid.Parse(input.UserId);

            // Códigos de incidencia que el usuario actual puede aprobar (está configurado como aprobador).
            var myApprovers = _incidentApproverRepository.GetByFilter(ia => ia.UserId == userId).ToList();
            var myApproverCodes = myApprovers.Select(ia => ia.IncidentCode).ToHashSet();

            if (myApproverCodes.Count == 0)
            {
                return Enumerable.Empty<PendingIncidenceApprovalOutput>();
            }

            // Incidencias de esos códigos en la empresa, filtradas por estado:
            // -1 = Todas, 0 = Pendientes, 1 = Aprobadas, 2 = Rechazadas.
            var status = input.Status;
            var pendingIncidents = _repository.GetByFilter(ai =>
                ai.CompanyId == input.CompanyId &&
                myApproverCodes.Contains(ai.IncidentCode) &&
                (
                    status == -1 ||
                    (status == 0 && !ai.Approved && !ai.Rejected) ||
                    (status == 1 && ai.Approved) ||
                    (status == 2 && ai.Rejected)
                )
            ).ToList();

            if (pendingIncidents.Count == 0)
            {
                return Enumerable.Empty<PendingIncidenceApprovalOutput>();
            }

            // Los permisos que se registraron como solicitud de ausencia se aprueban en el flujo de
            // "Solicitudes de ausencia"; aquí solo se descartan esos. Los permisos que requieren
            // aprobación pero NO tienen una solicitud asociada sí deben aparecer en esta lista para
            // no quedar atascados (no aparecían en ninguna de las dos pestañas).
            var absenceRequests = _employeeAbsenceRequestsRepository
                .GetByFilter(r => r.CompanyId == input.CompanyId)
                .ToList();

            pendingIncidents = pendingIncidents
                .Where(ai => !ai.TimeOffRequest || !absenceRequests.Any(r =>
                    r.EmployeeCode == ai.EmployeeCode &&
                    r.IncidentCode == ai.IncidentCode &&
                    ai.Date >= r.StartDate &&
                    ai.Date <= r.EndDate))
                .ToList();

            if (pendingIncidents.Count == 0)
            {
                return Enumerable.Empty<PendingIncidenceApprovalOutput>();
            }

            // Filtrar por el centro/supervisor seleccionado (a menos que sea "TODOS" = -999).
            var tenant = _globalPropertyService.Tenant;
            if (!string.IsNullOrEmpty(tenant) && tenant != "-999" && tenant != "all")
            {
                var codes = pendingIncidents.Select(i => i.EmployeeCode).Distinct().ToList();
                var keysQuery = _keyRepository.GetContextEntity()
                    .Where(k => k.Company == input.CompanyId && codes.Contains((int)k.Codigo));

                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    keysQuery = keysQuery.Where(k => k.Center == tenant);
                }
                else
                {
                    var supervisorId = Convert.ToDecimal(tenant);
                    keysQuery = keysQuery.Where(k => k.Supervisor == supervisorId);
                }

                var allowedCodes = keysQuery.Select(k => (int)k.Codigo).ToHashSet();
                pendingIncidents = pendingIncidents.Where(i => allowedCodes.Contains(i.EmployeeCode)).ToList();

                if (pendingIncidents.Count == 0)
                {
                    return Enumerable.Empty<PendingIncidenceApprovalOutput>();
                }
            }

            var incidentIds = pendingIncidents.Select(i => i.Id).ToHashSet();
            var incidentCodes = pendingIncidents.Select(i => i.IncidentCode).ToHashSet();
            var employeeCodes = pendingIncidents.Select(i => i.EmployeeCode).ToHashSet();

            // Total de aprobadores requeridos por código y aprobaciones ya registradas por incidencia.
            var approversByCode = _incidentApproverRepository.GetByFilter(ia => incidentCodes.Contains(ia.IncidentCode))
                .GroupBy(ia => ia.IncidentCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            var approvals = _assistanceIncidentApproverRepository.GetByFilter(a => incidentIds.Contains(a.AssistanceIncidentId)).ToList();
            var approvalsByIncident = approvals
                .GroupBy(a => a.AssistanceIncidentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var myApproverIds = myApprovers.Select(ia => ia.Id).ToHashSet();

            var incidentCodeLabels = _incidentCodeRepository.GetByFilter(ic => incidentCodes.Contains(ic.Code))
                .ToDictionary(ic => ic.Code, ic => ic.Label);

            var employees = _employeeRepository.GetByFilter(e => e.Company == input.CompanyId && employeeCodes.Contains((int)e.Codigo))
                .ToDictionary(e => (int)e.Codigo, e => $"{e.Name} {e.LastName} {e.MLastName}");

            return pendingIncidents.Select(incident =>
            {
                approversByCode.TryGetValue(incident.IncidentCode, out var codeApprovers);
                approvalsByIncident.TryGetValue(incident.Id, out var incidentApprovals);

                var totalApprovers = codeApprovers?.Count ?? 0;
                var approvedCount = incidentApprovals?.Count ?? 0;
                var alreadyApprovedByMe = incidentApprovals?.Any(a => myApproverIds.Contains(a.IncidentApproverId)) ?? false;

                return new PendingIncidenceApprovalOutput
                {
                    Id = incident.Id,
                    RequestGroupId = incident.RequestGroupId,
                    EmployeeCode = incident.EmployeeCode,
                    EmployeeName = employees.TryGetValue(incident.EmployeeCode, out var name) ? name : string.Empty,
                    IncidentCode = incident.IncidentCode,
                    IncidentDescription = incidentCodeLabels.TryGetValue(incident.IncidentCode, out var label) ? label : string.Empty,
                    Date = incident.Date,
                    Notes = incident.Notes,
                    CreatedAt = incident.CreatedAt,
                    TotalApprovers = totalApprovers,
                    ApprovedCount = approvedCount,
                    AlreadyApprovedByMe = alreadyApprovedByMe,
                    Approved = incident.Approved,
                    Rejected = incident.Rejected,
                };
            })
            // Las que el usuario actual ya aprobó siguen visibles (esperando a otros aprobadores),
            // pero el frontend les oculta las acciones de aprobar/rechazar usando AlreadyApprovedByMe.
            .OrderBy(o => o.Date)
            .ToList();
        }

        public AssistanceIncident ExecuteProcess(ApproveIncidence input)
        {
            if (string.IsNullOrEmpty(input.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var userId = Guid.Parse(input.UserId);

            var incident = _repository.GetByFilter(i => i.Id == input.AssistanceIncidentId && i.CompanyId == input.CompanyId).FirstOrDefault();

            if (incident == null)
            {
                throw new BadHttpRequestException("La incidencia no existe.");
            }

            if (incident.Rejected)
            {
                throw new BadHttpRequestException("La incidencia fue rechazada y no puede aprobarse.");
            }

            if (incident.Approved)
            {
                return incident;
            }

            // El usuario debe estar configurado como aprobador del código de la incidencia.
            var myApprover = _incidentApproverRepository.GetByFilter(ia =>
                ia.IncidentCode == incident.IncidentCode && ia.UserId == userId
            ).FirstOrDefault();

            if (myApprover == null)
            {
                throw new BadHttpRequestException("No tienes autorización para aprobar esta incidencia.");
            }

            var existingApprovals = _assistanceIncidentApproverRepository
                .GetByFilter(a => a.AssistanceIncidentId == incident.Id).ToList();

            // Evitar doble aprobación del mismo usuario.
            if (!existingApprovals.Any(a => a.IncidentApproverId == myApprover.Id))
            {
                _assistanceIncidentApproverRepository.Create(new AssistanceIncidentApprover
                {
                    AssistanceIncidentId = incident.Id,
                    IncidentApproverId = myApprover.Id,
                    ApprovalDate = DateTime.UtcNow,
                    AssistanceIncident = incident,
                    IncidentApprover = myApprover,
                });
                _assistanceIncidentApproverRepository.Save();

                existingApprovals.Add(new AssistanceIncidentApprover
                {
                    AssistanceIncidentId = incident.Id,
                    IncidentApproverId = myApprover.Id,
                    ApprovalDate = DateTime.UtcNow,
                    AssistanceIncident = incident,
                    IncidentApprover = myApprover,
                });
            }

            // La incidencia queda aprobada sólo cuando TODOS los aprobadores configurados aprobaron.
            var totalApprovers = _incidentApproverRepository.GetByFilter(ia => ia.IncidentCode == incident.IncidentCode).Count();
            var approvedCount = existingApprovals.Select(a => a.IncidentApproverId).Distinct().Count();

            if (totalApprovers > 0 && approvedCount >= totalApprovers)
            {
                incident.Approved = true;
                incident.Rejected = false;
                incident.UpdatedAt = DateTime.UtcNow;
                _repository.Update(incident);
                _repository.Save();

                _auditLogRepository.Create(new AuditLog()
                {
                    SectionCode = "TASISTENCIA",
                    RecordId = incident.Id.ToString(),
                    Description = $"Se aprobó por completo la incidencia {incident.IncidentCode} del empleado {incident.EmployeeCode} de la empresa {incident.CompanyId} el día {incident.Date}.",
                    OldValue = "Pendiente",
                    NewValue = "Aprobado",
                    ByUserId = userId,
                });
                _auditLogRepository.Save();
            }

            return incident;
        }

        public AssistanceIncident ExecuteProcess(RejectIncidence input)
        {
            if (string.IsNullOrEmpty(input.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var userId = Guid.Parse(input.UserId);

            var incident = _repository.GetByFilter(i => i.Id == input.AssistanceIncidentId && i.CompanyId == input.CompanyId).FirstOrDefault();

            if (incident == null)
            {
                throw new BadHttpRequestException("La incidencia no existe.");
            }

            // El usuario debe estar configurado como aprobador del código de la incidencia.
            var myApprover = _incidentApproverRepository.GetByFilter(ia =>
                ia.IncidentCode == incident.IncidentCode && ia.UserId == userId
            ).FirstOrDefault();

            if (myApprover == null)
            {
                throw new BadHttpRequestException("No tienes autorización para rechazar esta incidencia.");
            }

            incident.Rejected = true;
            incident.Approved = false;
            incident.RejectionComment = input.Comment;
            incident.RejectedByUserId = userId;
            incident.RejectedAt = DateTime.UtcNow;
            incident.UpdatedAt = DateTime.UtcNow;

            _repository.Update(incident);
            _repository.Save();

            _auditLogRepository.Create(new AuditLog()
            {
                SectionCode = "TASISTENCIA",
                RecordId = incident.Id.ToString(),
                Description = $"Se rechazó la incidencia {incident.IncidentCode} del empleado {incident.EmployeeCode} de la empresa {incident.CompanyId} el día {incident.Date}. Motivo: {input.Comment ?? "Sin comentario"}",
                OldValue = "Pendiente",
                NewValue = "Rechazado",
                ByUserId = userId,
            });
            _auditLogRepository.Save();

            return incident;
        }

        public List<AssistanceIncident> ExecuteProcess(ApproveIncidenceGroup input)
        {
            if (string.IsNullOrEmpty(input.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            // Group incidences that are still pending (neither approved nor rejected).
            var groupIncidents = _repository.GetByFilter(i =>
                i.RequestGroupId == input.RequestGroupId &&
                i.CompanyId == input.CompanyId &&
                !i.Approved && !i.Rejected
            ).ToList();

            if (groupIncidents.Count == 0)
            {
                throw new BadHttpRequestException("No hay incidencias pendientes en este grupo.");
            }

            // The single-incidence logic is reused for each incidence in the group.
            return groupIncidents.Select(incident => ExecuteProcess(new ApproveIncidence
            {
                AssistanceIncidentId = incident.Id,
                CompanyId = input.CompanyId,
                UserId = input.UserId,
            })).ToList();
        }

        public List<AssistanceIncident> ExecuteProcess(RejectIncidenceGroup input)
        {
            if (string.IsNullOrEmpty(input.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            // Group incidences that are still pending (neither approved nor rejected).
            var groupIncidents = _repository.GetByFilter(i =>
                i.RequestGroupId == input.RequestGroupId &&
                i.CompanyId == input.CompanyId &&
                !i.Approved && !i.Rejected
            ).ToList();

            if (groupIncidents.Count == 0)
            {
                throw new BadHttpRequestException("No hay incidencias pendientes en este grupo.");
            }

            // The single-incidence logic is reused for each incidence in the group.
            return groupIncidents.Select(incident => ExecuteProcess(new RejectIncidence
            {
                AssistanceIncidentId = incident.Id,
                Comment = input.Comment,
                CompanyId = input.CompanyId,
                UserId = input.UserId,
            })).ToList();
        }

        public bool ExecuteProcess(DeleteIncidentsToEmployee deleteIncidentsToEmployee)
        {
            var listIncidents = deleteIncidentsToEmployee.IncidentIds.ToList();

            var incidents = _repository.GetByFilter(r => listIncidents.Contains(r.Id.ToString())).ToList();

            foreach (var item in incidents)
            {
                var key = _keyRepository.GetByFilter(k => k.Codigo == item.EmployeeCode && k.Company == item.CompanyId).FirstOrDefault();

                if (key == null)
                {
                    throw new BadHttpRequestException("El empleado no se encuentra en la empresa seleccionada.");
                }

                var findPeriod = _periodService.ExecuteProcess<FindPeriod, Models.Prenomina.Period?>(new FindPeriod()
                {
                    CompanyId = item.CompanyId,
                    Date = item.Date,
                    TypePayroll = key.TypeNom,
                    Year = _globalPropertyService.YearOfOperation,
                });

                if (findPeriod == null)
                {
                    throw new BadHttpRequestException("La fecha no se encuentra en ningun periodo.");
                }

                var closedPeriod = _periodService.ExecuteProcess<VerifyClosedPeriod, bool>(new VerifyClosedPeriod()
                {
                    CompanyId = item.CompanyId,
                    TenantId = _globalPropertyService.TypeTenant == TypeTenant.Department ? key.Center.Trim() : key.Supervisor.ToString(),
                    NumPeriod = findPeriod.NumPeriod,
                    TypePayroll = key.TypeNom,
                    Year = _globalPropertyService.YearOfOperation,
                });

                if (closedPeriod)
                {
                    throw new BadHttpRequestException("El periodo se encuentra cerrado.");
                }

                _repository.Delete(item);

                _auditLogRepository.Create(new AuditLog()
                {
                    SectionCode = SectionCode.Attendance,
                    RecordId = item.Id.ToString(),
                    Description = $"Se elimino la incidencia de asistencia para el empleado {item.EmployeeCode} de la empresa {item.CompanyId} el día {item.Date}.",
                    OldValue = item.IncidentCode,
                    NewValue = "",
                    ByUserId = Guid.Parse(deleteIncidentsToEmployee.UserId!),
                });
            }

            _repository.Save();
            _auditLogRepository.Save();

            return true;
        }

        public AssistanceIncident ExecuteProcess(AssignDoubleShift assignDoubleShift)
        {
            var key = _keyRepository.GetByFilter(k => k.Codigo == assignDoubleShift.EmployeeCode && k.Company == assignDoubleShift.CompanyId).FirstOrDefault();

            if (key == null)
            {
                throw new BadHttpRequestException("El empleado no se encuentra en la empresa seleccionada.");
            }

            var findPeriod = _periodService.ExecuteProcess<FindPeriod, Models.Prenomina.Period?>(new FindPeriod()
            {
                CompanyId = assignDoubleShift.CompanyId,
                Date = assignDoubleShift.Date,
                TypePayroll = key.TypeNom,
                Year = _globalPropertyService.YearOfOperation,
            });

            if (findPeriod == null)
            {
                throw new BadHttpRequestException("La fecha no se encuentra en ningun periodo.");
            }

            var closedPeriod = _periodService.ExecuteProcess<VerifyClosedPeriod, bool>(new VerifyClosedPeriod()
            {
                CompanyId = assignDoubleShift.CompanyId,
                TenantId = _globalPropertyService.TypeTenant == TypeTenant.Department ? key.Center.Trim() : key.Supervisor.ToString(),
                NumPeriod = findPeriod.NumPeriod,
                TypePayroll = key.TypeNom,
                Year = _globalPropertyService.YearOfOperation,
            });

            if (closedPeriod)
            {
                throw new BadHttpRequestException("El periodo se encuentra cerrado.");
            }

            var incidentCode = DefaultIncidentCodes.DobleTurno;

            return this.ExecuteProcess<ApplyIncident, AssistanceIncident>(new ApplyIncident()
            {
                IncidentCode = incidentCode,
                CompanyId = assignDoubleShift.CompanyId,
                Date = assignDoubleShift.Date,
                EmployeeCode = assignDoubleShift.EmployeeCode,
                UserId = assignDoubleShift.UserId,
            });
        }

        public bool ExecuteProcess(DeleteCheckins deleteCheckins)
        {
            if (deleteCheckins.CheckEntryId == null && deleteCheckins.CheckOutId == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var userId = Guid.Parse(deleteCheckins.UserId!);

            void SoftDelete(string id, string eosLabel)
            {
                var record = _employeeCheckInsRepository.GetById(Guid.Parse(id));
                if (record == null) return;

                record.DeletedAt = now;
                record.UpdatedAt = now;
                _employeeCheckInsRepository.Update(record);

                _auditLogRepository.Create(new AuditLog()
                {
                    SectionCode = "TASISTENCIA",
                    RecordId = id,
                    Description = $"Se eliminó la {eosLabel} de asistencia para el empleado {record.EmployeeCode} de la empresa {record.CompanyId} el día {record.Date:dd-MM-yyyy}.",
                    OldValue = record.CheckIn.ToString("HH:mm:ss"),
                    NewValue = "",
                    ByUserId = userId,
                });
            }

            if (deleteCheckins.CheckEntryId != null) SoftDelete(deleteCheckins.CheckEntryId, "Entrada");
            if (deleteCheckins.CheckOutId != null) SoftDelete(deleteCheckins.CheckOutId, "Salida");

            _employeeCheckInsRepository.Save();
            _auditLogRepository.Save();

            return true;
        }

        public bool ExecuteProcess(ChangeAttendance changeAttendance)
        {
            // Validar que el periodo no esté cerrado antes de modificar checadas (aplica a TODOS los roles, incluido admin)
            var keyForPeriod = _keyRepository.GetContextEntity()
                .FirstOrDefault(k => k.Codigo == changeAttendance.EmployeeCode && k.Company == changeAttendance.CompanyId);

            if (keyForPeriod != null)
            {
                var periodForCheck = _periodService.ExecuteProcess<FindPeriod, Models.Prenomina.Period?>(new FindPeriod()
                {
                    CompanyId = (int)changeAttendance.CompanyId,
                    Date = changeAttendance.Date,
                    TypePayroll = keyForPeriod.TypeNom,
                    Year = _globalPropertyService.YearOfOperation,
                });

                if (periodForCheck != null)
                {
                    var isClosed = _periodService.ExecuteProcess<VerifyClosedPeriod, bool>(new VerifyClosedPeriod()
                    {
                        CompanyId = (int)changeAttendance.CompanyId,
                        TenantId = _globalPropertyService.TypeTenant == TypeTenant.Department ? keyForPeriod.Center.Trim() : keyForPeriod.Supervisor.ToString(),
                        NumPeriod = periodForCheck.NumPeriod,
                        TypePayroll = keyForPeriod.TypeNom,
                        Year = _globalPropertyService.YearOfOperation,
                    });

                    if (isClosed)
                    {
                        throw new BadHttpRequestException("El periodo está cerrado. No se pueden modificar las checadas.");
                    }
                }
            }

            // Cambiar la asistencia de entrada
            if (changeAttendance.CheckEntryId != null)
            {
                var attendanceEntry = _employeeCheckInsRepository.GetById(Guid.Parse(changeAttendance.CheckEntryId));
                if (attendanceEntry != null)
                {
                    var oldValue = attendanceEntry.CheckIn;

                    attendanceEntry.CheckIn = TimeOnly.Parse(changeAttendance.CheckEntry);
                    attendanceEntry.UpdatedAt = DateTime.UtcNow;
                    attendanceEntry.EoS = EntryOrExit.Entry;
                    _employeeCheckInsRepository.Update(attendanceEntry);
                    _employeeCheckInsRepository.Save();

                    _auditLogRepository.Create(new AuditLog()
                    {
                        SectionCode = "TASISTENCIA",
                        RecordId = changeAttendance.CheckEntryId,
                        Description = $"Se modificó la Entrada de asistencia para el empleado {changeAttendance.EmployeeCode} de la empresa {changeAttendance.CompanyId} el día {changeAttendance.Date.ToString("dd-MM-yyyy")}.",
                        OldValue = oldValue.ToString("HH:mm:ss"),
                        NewValue = changeAttendance.CheckEntry,
                        ByUserId = Guid.Parse(changeAttendance.UserId!),
                    });
                    _auditLogRepository.Save();
                }
            } else
            {
                var attendanceEntry = new EmployeeCheckIns()
                {
                    CompanyId = changeAttendance.CompanyId,
                    EmployeeCode = int.Parse(changeAttendance.EmployeeCode.ToString()),
                    Date = changeAttendance.Date,
                    CheckIn = TimeOnly.Parse(changeAttendance.CheckEntry),
                    CreatedAt = DateTime.UtcNow,
                    EmployeeSchedule = 0,
                    EoS = EntryOrExit.Entry,
                    NumConc = "",
                    Period = 0,
                    TypeNom = 0,
                    UpdatedAt = DateTime.UtcNow,
                };
                _employeeCheckInsRepository.Create(attendanceEntry);
                _employeeCheckInsRepository.Save();
            }


            // Para turnos nocturnos, si la hora de salida es de madrugada (< StartTime),
            // la salida pertenece al día siguiente (cruza medianoche).
            var checkOutTime = TimeOnly.Parse(changeAttendance.CheckOut);
            var exitDate = changeAttendance.Date;
            var employeeSchedule = _scheduleResolver.GetScheduleForEmployee(
                (int)changeAttendance.EmployeeCode,
                (int)changeAttendance.CompanyId,
                changeAttendance.Date);

            if (employeeSchedule != null && employeeSchedule.IsNightShift && checkOutTime < employeeSchedule.StartTime)
            {
                exitDate = changeAttendance.Date.AddDays(1);
            }

            // Cambiar la asistencia de salida
            if (changeAttendance.CheckOutId != null)
            {
                var attendanceExit = _employeeCheckInsRepository.GetById(Guid.Parse(changeAttendance.CheckOutId));
                if (attendanceExit != null)
                {
                    var oldValue = attendanceExit.CheckIn;
                    attendanceExit.CheckIn = checkOutTime;
                    attendanceExit.Date = exitDate;
                    attendanceExit.UpdatedAt = DateTime.UtcNow;
                    attendanceExit.EoS = EntryOrExit.Exit;

                    _employeeCheckInsRepository.Update(attendanceExit);
                    _employeeCheckInsRepository.Save();
                    _auditLogRepository.Create(new AuditLog()
                    {
                        SectionCode = "TASISTENCIA",
                        RecordId = changeAttendance.CheckOutId,
                        Description = $"Se modificó la Salida de asistencia para el empleado {changeAttendance.EmployeeCode} de la empresa {changeAttendance.CompanyId} el día {changeAttendance.Date.ToString("dd-MM-yyyy")}.",
                        OldValue = oldValue.ToString("HH:mm:ss"),
                        NewValue = changeAttendance.CheckOut,
                        ByUserId = Guid.Parse(changeAttendance.UserId!),
                    });
                    _auditLogRepository.Save();
                }
            }
            else
            {
                var attendanceExit = new EmployeeCheckIns()
                {
                    CompanyId = changeAttendance.CompanyId,
                    EmployeeCode = int.Parse(changeAttendance.EmployeeCode.ToString()),
                    Date = exitDate,
                    CheckIn = checkOutTime,
                    CreatedAt = DateTime.UtcNow,
                    EmployeeSchedule = 0,
                    EoS = EntryOrExit.Exit,
                    NumConc = "",
                    Period = 0,
                    TypeNom = 0,
                    UpdatedAt = DateTime.UtcNow,
                };

                _employeeCheckInsRepository.Create(attendanceExit);
                _employeeCheckInsRepository.Save();
            }


            return true;
        }

        public FixNightShiftEoSResult ExecuteProcess(FixNightShiftEoS input)
        {
            var context = _repository.GetDbContext();

            var nightScheduleAssignments = context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Include(a => a.WorkSchedule)
                .Where(a => a.CompanyId == input.CompanyId
                    && a.DeletedAt == null
                    && a.WorkSchedule != null
                    && a.WorkSchedule.IsNightShift
                    && a.WorkSchedule.DeletedAt == null)
                .ToList();

            if (!nightScheduleAssignments.Any())
            {
                return new FixNightShiftEoSResult
                {
                    TotalFixed = 0,
                    Message = "No se encontraron empleados con turno nocturno."
                };
            }

            // Un empleado puede tener varias asignaciones de turno nocturno en periodos
            // distintos; las conservamos todas y resolvemos por fecha de la checada.
            var assignmentsByEmployee = nightScheduleAssignments
                .GroupBy(a => a.EmployeeCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(a => a.EffectiveFrom).ToList()
                );

            var employeeCodes = assignmentsByEmployee.Keys.ToList();

            var checkIns = _employeeCheckInsRepository.GetContextEntity()
                .Where(ci => employeeCodes.Contains(ci.EmployeeCode)
                    && ci.CompanyId == input.CompanyId
                    && ci.DeletedAt == null)
                .ToList();

            int totalFixed = 0;

            // Reclasifica cada checada ignorando el EoS actual, usando el mismo
            // clasificador que ClockService aplica al ingerir checadas del reloj.
            foreach (var ci in checkIns)
            {
                if (!assignmentsByEmployee.TryGetValue(ci.EmployeeCode, out var assignments))
                    continue;

                var assignment = assignments.FirstOrDefault(a =>
                    ci.Date >= a.EffectiveFrom && (a.EffectiveTo == null || ci.Date <= a.EffectiveTo));

                if (assignment?.WorkSchedule == null)
                    continue;

                var correctEoS = WorkScheduleClassifier.ClassifyBySchedule(ci.CheckIn, assignment.WorkSchedule);

                if (ci.EoS != correctEoS)
                {
                    ci.EoS = correctEoS;
                    ci.UpdatedAt = DateTime.UtcNow;
                    _employeeCheckInsRepository.Update(ci);
                    totalFixed++;
                }
            }

            if (totalFixed > 0)
            {
                _employeeCheckInsRepository.Save();
            }

            return new FixNightShiftEoSResult
            {
                TotalFixed = totalFixed,
                Message = $"Se corrigieron {totalFixed} clasificaciones de checadas nocturnas."
            };
        }
    }
}
