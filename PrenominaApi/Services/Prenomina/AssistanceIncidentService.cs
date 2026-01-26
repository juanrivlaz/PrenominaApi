using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Attendance;
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
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToActivity> _ignoreIncidentToActivityRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeCheckIns> _employeeCheckInsRepository;
        private readonly IBaseRepositoryPrenomina<User> _userRepository;

        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodService;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Employee> _employeeRepository;
        private readonly GlobalPropertyService _globalPropertyService;

        public AssistanceIncidentService(
            IBaseRepositoryPrenomina<AssistanceIncident> repository,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<AuditLog> auditLogRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> ignoreIncidentToEmployeeRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToActivity> ignoreIncidentToActivityRepository,
            IBaseRepositoryPrenomina<EmployeeCheckIns> employeeCheckInsRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodService,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Employee> employeeRepository,
            GlobalPropertyService globalPropertyService
        ) : base(repository) {
            _incidentCodeRepository = incidentCodeRepository;
            _auditLogRepository = auditLogRepository;
            _ignoreIncidentToEmployeeRepository = ignoreIncidentToEmployeeRepository;
            _ignoreIncidentToActivityRepository = ignoreIncidentToActivityRepository;
            _employeeCheckInsRepository = employeeCheckInsRepository;
            _userRepository = userRepository;

            _keyRepository = keyRepository;
            _employeeRepository = employeeRepository;
            _globalPropertyService = globalPropertyService;
            _periodService = periodService;
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
                throw new BadHttpRequestException("El código de incidencia se encuentra ignorado para este usuario.");
            }

            using var transaction = _repository.GetDbContext().Database.BeginTransaction();

            try
            {
                if (findAssistanceIncident != null && !findIncidentCode.IsAdditional)
                {
                    var prevIncidentCode = findAssistanceIncident.IncidentCode;
                    findAssistanceIncident.IncidentCode = applyIncident.IncidentCode;
                    findAssistanceIncident.UpdatedAt = DateTime.UtcNow;

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

        public bool ExecuteProcess(ChangeAttendance changeAttendance)
        {
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


            // Cambiar la asistencia de salida
            if (changeAttendance.CheckOutId != null)
            {
                var attendanceExit = _employeeCheckInsRepository.GetById(Guid.Parse(changeAttendance.CheckOutId));
                if (attendanceExit != null)
                {
                    var oldValue = attendanceExit.CheckIn;
                    attendanceExit.CheckIn = TimeOnly.Parse(changeAttendance.CheckOut);
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
                    Date = changeAttendance.Date,
                    CheckIn = TimeOnly.Parse(changeAttendance.CheckOut),
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
    }
}
