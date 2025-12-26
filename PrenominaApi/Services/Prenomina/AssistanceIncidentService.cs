using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class AssistanceIncidentService : ServicePrenomina<AssistanceIncident>
    {
        private readonly IBaseRepositoryPrenomina<IncidentCode> _incidentCodeRepository;
        private readonly IBaseRepositoryPrenomina<AuditLog> _auditLogRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> _ignoreIncidentToEmployeeRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToTenant> _ignoreIncidentToTenantRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToActivity> _ignoreIncidentToActivityRepository;
        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodService;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly GlobalPropertyService _globalPropertyService;

        public AssistanceIncidentService(
            IBaseRepositoryPrenomina<AssistanceIncident> repository,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<AuditLog> auditLogRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToTenant> ignoreIncidentToTenantRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> ignoreIncidentToEmployeeRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToActivity> ignoreIncidentToActivityRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodService,
            IBaseRepository<Key> keyRepository,
            GlobalPropertyService globalPropertyService
        ) : base(repository) {
            _incidentCodeRepository = incidentCodeRepository;
            _auditLogRepository = auditLogRepository;
            _ignoreIncidentToEmployeeRepository = ignoreIncidentToEmployeeRepository;
            _ignoreIncidentToTenantRepository = ignoreIncidentToTenantRepository;
            _ignoreIncidentToActivityRepository = ignoreIncidentToActivityRepository;
            _keyRepository = keyRepository;
            _globalPropertyService = globalPropertyService;
            _periodService = periodService;
        }

        public AssistanceIncident ExecuteProcess(ApplyIncident applyIncident)
        {

            var findIncidentCode = _incidentCodeRepository.GetById( applyIncident.IncidentCode );

            if (findIncidentCode == null) {
                throw new BadHttpRequestException("El código de incidencia no existe");
            }

            var findAssistanceIncident = _repository.GetByFilter((item) => item.CompanyId == applyIncident.CompanyId && item.EmployeeCode == applyIncident.EmployeeCode && item.Date == applyIncident.Date).FirstOrDefault();

            if (findIncidentCode.IsAdditional && findAssistanceIncident != null && findAssistanceIncident.IncidentCode == applyIncident.IncidentCode && findAssistanceIncident.Date == applyIncident.Date)
            {
                throw new BadHttpRequestException("El código de incidencia ya está registrado para este empleado en la misma fecha.");
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
                throw new BadHttpRequestException("El código de incidencia se encuentra ignorado para este usuario.");
            }

            if (findAssistanceIncident != null && !findIncidentCode.IsAdditional) {
                var prevIncidentCode = findAssistanceIncident.IncidentCode;
                findAssistanceIncident.IncidentCode = applyIncident.IncidentCode;
                findAssistanceIncident.UpdatedAt = DateTime.UtcNow;

                _repository.Update( findAssistanceIncident );

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
            } else
            {
                findAssistanceIncident = _repository.Create(new AssistanceIncident()
                {
                    CompanyId = applyIncident.CompanyId,
                    EmployeeCode = applyIncident.EmployeeCode,
                    Date = applyIncident.Date,
                    IncidentCode = applyIncident.IncidentCode,
                    TimeOffRequest = false,
                    Approved = !findIncidentCode.RequiredApproval,
                    ByUserId = Guid.Parse(applyIncident.UserId!)
                });
            }

            _repository.Save();
            _auditLogRepository.Save();

            return findAssistanceIncident;
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
    }
}
