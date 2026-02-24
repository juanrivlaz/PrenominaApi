using PrenominaApi.Models.Dto;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Repositories;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using PrenominaApi.Services.Utilities;

namespace PrenominaApi.Services.Prenomina
{
    public class DayOffsService : ServicePrenomina<DayOff>
    {
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly PDFService _pdfService;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Employee> _employeeRepository;
        private readonly IBaseRepository<Company> _companyRepository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRepository;
        private readonly IBaseRepository<Payroll> _payrollRepository;
        private readonly IBaseRepository<Deduction> _deductionRepository;
        private readonly IBaseRepository<Kardex> _kardexRepository;
        private readonly IBaseRepositoryPrenomina<Models.Prenomina.Period> _periodRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncident> _assistanceIncident;
        private readonly IBaseRepositoryPrenomina<IncidentCode> _incidentCodeRepository;
        private readonly IBaseRepositoryPrenomina<AuditLog> _auditLogRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeCheckIns> _employeeCheckInsRepository;
        private readonly IBaseServicePrenomina<User> _userService;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> _ignoreIncidentToEmployeeRepository;
        private readonly IBaseRepositoryPrenomina<IgnoreIncidentToActivity> _ignoreIncidentToActivityRepository;
        private readonly IBaseServicePrenomina<EmployeeAbsenceRequests> _employeeAbsenRequestService;
        private readonly IBaseServicePrenomina<SystemConfig> _sysConfigService;

        public DayOffsService(
            GlobalPropertyService globalPropertyService,
            PDFService pdfService,
            IBaseRepository<Employee> employeeRepository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Company> companyRepository,
            IBaseRepository<Center> centerRepository,
            IBaseRepository<Supervisor> supervisorRepository,
            IBaseRepository<Payroll> payrollRepository,
            IBaseRepository<Deduction> deductionRepository,
            IBaseRepository<Kardex> kardexRepository,
            IBaseRepositoryPrenomina<Models.Prenomina.Period> periodRepository,
            IBaseRepositoryPrenomina<DayOff> baseRepository,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncident,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<AuditLog> auditLogRepository,
            IBaseRepositoryPrenomina<EmployeeCheckIns> employeeCheckInsRepository,
            IBaseServicePrenomina<User> userService,
            IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> ignoreIncidentToEmployeeRepository,
            IBaseRepositoryPrenomina<IgnoreIncidentToActivity> ignoreIncidentToActivityRepository,
            IBaseServicePrenomina<SystemConfig> sysConfigService,
            IBaseServicePrenomina<EmployeeAbsenceRequests> employeeAbsenRequestService
        ) : base(baseRepository) {
            _globalPropertyService = globalPropertyService;
            _keyRepository = keyRepository;
            _employeeRepository = employeeRepository;
            _assistanceIncident = assistanceIncident;
            _incidentCodeRepository = incidentCodeRepository;
            _auditLogRepository = auditLogRepository;
            _periodRepository = periodRepository;
            _companyRepository = companyRepository;
            _centerRepository = centerRepository;
            _supervisorRepository = supervisorRepository;
            _payrollRepository = payrollRepository;
            _employeeCheckInsRepository = employeeCheckInsRepository;
            _pdfService = pdfService;
            _userService = userService;
            _deductionRepository = deductionRepository;
            _kardexRepository = kardexRepository;
            _ignoreIncidentToEmployeeRepository = ignoreIncidentToEmployeeRepository;
            _ignoreIncidentToActivityRepository = ignoreIncidentToActivityRepository;
            _sysConfigService = sysConfigService;
            _employeeAbsenRequestService = employeeAbsenRequestService;
        }

        public DayOff ExecuteProcess(CreateDayOff createDayOff)
        {
            var monthDay = createDayOff.Date.ToString("MMdd");
            var existDayOff = _repository.GetByFilter(item => item.Date.ToString("MMdd") == monthDay).FirstOrDefault();

            if (existDayOff != null)
            {
                throw new BadHttpRequestException("La fecha ya se encuentra registrada");
            }

            var result = _repository.Create(new DayOff()
            {
                Description = createDayOff.Description,
                IncidentCode = createDayOff.IncidentCode,
                Date = createDayOff.Date,
                IsUnion = createDayOff.IsUnion,
            });

            _repository.Save();

            return result;
        }

        public DayOff ExecuteProcess(EditDayOff editDayOff)
        {
            var dayoff = _repository.GetById(editDayOff.Id);

            if (dayoff == null)
            {
                throw new KeyNotFoundException("El registro seleccionado no existe");
            }

            var monthDay = editDayOff.Date;
            var existDayOff = _repository.GetByFilter(item => item.Date.Month == monthDay.Month && item.Date.Day == monthDay.Day && item.Id != editDayOff.Id).FirstOrDefault();
            if (existDayOff != null)
            {
                throw new InvalidOperationException("La fecha ya se encuentra registrada");
            }

            dayoff.Date = editDayOff.Date;
            dayoff.Description = editDayOff.Description;
            dayoff.IncidentCode = editDayOff.IncidentCode;
            dayoff.IsUnion = editDayOff.IsUnion;

            var result = _repository.Update(dayoff);

            _repository.Save();

            return result;
        }

        public DayOff ExecuteProcess(DeleteDayOff deleteDayOff)
        {
            var dayoff = _repository.GetById(Guid.Parse(deleteDayOff.Id));

            if (dayoff == null)
            {
                throw new KeyNotFoundException("El registro seleccionado no existe");
            }

            _repository.Delete(dayoff);
            _repository.Save();

            return dayoff;
        }

        public IEnumerable<WorkedDayOffs> ExecuteProcess(GetWorkedDayOff getWorkedDayOff)
        {
            List<WorkedDayOffs> result = new List<WorkedDayOffs>();
            IQueryable<Key> keys;
            int year = _globalPropertyService.YearOfOperation;
            var ignoreIncidentEmployee = _ignoreIncidentToEmployeeRepository.GetByFilter(ie => ie.Ignore == true && ie.IncidentCode == DefaultIncidentCodes.DescansoLaborado);
            var ignoreIncidentActivity = _ignoreIncidentToActivityRepository.GetByFilter(ie => ie.Ignore == true && ie.IncidentCode == DefaultIncidentCodes.DescansoLaborado);
            var employeeIgnore = ignoreIncidentEmployee.Select(e => e.EmployeeCode).ToList();
            var activityIgnore = ignoreIncidentActivity.Select(e => e.ActivityId).ToList();

            var dayoff = _repository.GetContextEntity().Include(d => d.IncidentCodeItem).ThenInclude(ic => ic == null ? null : ic.IncidentCodeMetadata).Where(d => d.Id == Guid.Parse(getWorkedDayOff.DayOffId)).FirstOrDefault();

            if (dayoff == null)
            {
                throw new KeyNotFoundException("La fecha seleccionado no existe");
            }

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == getWorkedDayOff.CompanyId &&
                    (getWorkedDayOff.Tenant != "-999" ? item.Center.Trim() == getWorkedDayOff.Tenant : true)
                );
            }
            else
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == getWorkedDayOff.CompanyId && 
                    (getWorkedDayOff.Tenant != "-999" ? item.Supervisor == Convert.ToDecimal(getWorkedDayOff.Tenant) : true)
                );
            }

            keys = keys.Include(k => k.Tabulator);
            var listKeys = keys.Where(k => activityIgnore.Any() ? !activityIgnore.Contains(k.Ocupation) : true).ToList();

            var employeeCodes = keys.Select(k => k.Codigo).ToList();
            Func<Employee, bool> filter = employee => employee.Company == getWorkedDayOff.CompanyId && employeeCodes.Contains(employee.Codigo) && (employeeIgnore.Any() ? !employeeIgnore.Contains((int)employee.Codigo) : true);
            var employees = _employeeRepository.GetByFilter(filter).ToList();

            List<GroupAttendaceRecords> attendanceRecord = new List<GroupAttendaceRecords>();

            var query = _employeeCheckInsRepository.GetDbContext().employeeCheckIns
            .Where(r =>
                r.CompanyId == getWorkedDayOff.CompanyId &&
                employeeCodes.Contains(r.EmployeeCode) &&
                r.Date.Year == year &&
                r.CheckIn != TimeOnly.MinValue
            );

            if (dayoff.IsSunday)
            {
                throw new KeyNotFoundException("La fecha no es valida para esta sección, redirigete a prima dominical.");
            }
            else
            {
                query = query.Where(r =>
                    r.Date.Month == dayoff.Date.Month &&
                    r.Date.Day == dayoff.Date.Day
                );
            }

            attendanceRecord = query
                .GroupBy(r => new { r.EmployeeCode, r.Date })
                .Select(g => new GroupAttendaceRecords
                {
                    Codigo = g.Key.EmployeeCode,
                    Date = g.Key.Date
                })
                .ToList();

            decimal multiplicator = 1;
            MathOperation? mathOperation = null;

            if (dayoff.IncidentCodeItem != null && dayoff.IncidentCodeItem.WithOperation)
            {
                multiplicator = dayoff.IncidentCodeItem.IncidentCodeMetadata!.Amount;
                mathOperation = dayoff.IncidentCodeItem.IncidentCodeMetadata!.MathOperation;
            }

            foreach (var item in attendanceRecord)
            {
                var employee = employees.Where(e => e.Codigo == item.Codigo).First();
                var keyEmployee = listKeys.Where(k => k.Codigo == employee.Codigo).FirstOrDefault();
                var amount = employee.Salary;

                if (keyEmployee == null)
                {
                    continue;
                }

                if (mathOperation != null)
                {
                    switch (mathOperation) {
                        case MathOperation.Multiplication:
                            amount = amount * multiplicator;
                            break;
                        case MathOperation.Division:
                            amount = amount / multiplicator;
                            break;
                        case MathOperation.Subtraction:
                            amount = amount - multiplicator;
                            break;
                        case MathOperation.Addition:
                            amount = amount + multiplicator;
                            break;
                        default:
                            break;
                    }
                }

                result.Add(new WorkedDayOffs() {
                    EmployeeCode = employee.Codigo,
                    EmployeeName = $"{employee.Name} {employee.LastName} {employee.MLastName}",
                    EmployeeSalary = employee.Salary,
                    Amount = amount,
                    Date = item.Date,
                    EmployeeActivity = keyEmployee.Tabulator.Activity ?? "",
                    Hours = 8,
                    NumConcept = dayoff.IncidentCodeItem!.ExternalCode,
                });
            }

            return result;
        }

        public IEnumerable<WorkedDayOffs> ExecuteProcess(GetWorkedSunday getWorkedSunday)
        {
            List<WorkedDayOffs> result = new List<WorkedDayOffs>();
            IQueryable<Key> keys;
            var year = _globalPropertyService.YearOfOperation;
            var period = _periodRepository.GetByFilter(p => p.Company == getWorkedSunday.CompanyId && p.TypePayroll == getWorkedSunday.PayrollId && p.NumPeriod == getWorkedSunday.NumberPeriod && p.Year == year).FirstOrDefault();
            var dayoff = _repository.GetContextEntity().Include(d => d.IncidentCodeItem).ThenInclude(ic => ic == null ? null : ic.IncidentCodeMetadata).Where(d => d.IsSunday).FirstOrDefault();
            var ignoreIncidentEmployee = _ignoreIncidentToEmployeeRepository.GetByFilter(ie => ie.Ignore == true && ie.IncidentCode == DefaultIncidentCodes.PrimaDominical);
            var ignoreIncidentActivity = _ignoreIncidentToActivityRepository.GetByFilter(ie => ie.Ignore == true && ie.IncidentCode == DefaultIncidentCodes.PrimaDominical);
            var employeeIgnore = ignoreIncidentEmployee.Select(e => e.EmployeeCode).ToList();
            var activityIgnore = ignoreIncidentActivity.Select(e => e.ActivityId).ToList();

            if (dayoff == null)
            {
                throw new KeyNotFoundException("No existe la configuración de prima dominical");
            }

            if (period == null)
            {
                throw new KeyNotFoundException("El periodo seleccionado no existe");
            }

            var listDate = DateService.GetListDate(period.StartDate, period.ClosingDate);
            var onlySunday = listDate.Where(d => d.DayOfWeek == DayOfWeek.Sunday);

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == getWorkedSunday.CompanyId &&
                    getWorkedSunday.Tenant != "-999" ? item.Center.Trim() == getWorkedSunday.Tenant : true
                );
            }
            else
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == getWorkedSunday.CompanyId &&
                    (getWorkedSunday.Tenant != "-999" ? item.Supervisor == Convert.ToDecimal(getWorkedSunday.Tenant) : true)
                );
            }

            keys = keys.Include(k => k.Tabulator);
            var listKeys = keys.Where(k => activityIgnore.Any() ? !activityIgnore.Contains(k.Ocupation) : true).ToList();

            var employeeCodes = keys.Select(k => k.Codigo).ToList();
            Func<Employee, bool> filter = employee => employee.Company == getWorkedSunday.CompanyId && employeeCodes.Contains(employee.Codigo) && (employeeIgnore.Any() ? !employeeIgnore.Contains((int)employee.Codigo) : true);
            var employees = _employeeRepository.GetByFilter(filter).ToList();

            List<GroupAttendaceRecords> attendanceRecord = new List<GroupAttendaceRecords>();

            var query = _employeeCheckInsRepository.GetDbContext().employeeCheckIns
            .Where(r =>
                r.CompanyId == getWorkedSunday.CompanyId &&
                employeeCodes.Contains(r.EmployeeCode) &&
                r.CheckIn != TimeOnly.MinValue &&
                onlySunday.Contains(r.Date)
            );

            attendanceRecord = query
                .GroupBy(r => new { r.EmployeeCode, r.Date })
                .Select(g => new GroupAttendaceRecords
                {
                    Codigo = g.Key.EmployeeCode,
                    Date = g.Key.Date
                })
                .ToList();

            decimal multiplicator = 1;
            MathOperation? mathOperation = null;

            if (dayoff.IncidentCodeItem != null && dayoff.IncidentCodeItem.WithOperation)
            {
                multiplicator = dayoff.IncidentCodeItem.IncidentCodeMetadata!.Amount;
                mathOperation = dayoff.IncidentCodeItem.IncidentCodeMetadata!.MathOperation;
            }

            foreach (var item in attendanceRecord)
            {
                var employee = employees.Where(e => e.Codigo == item.Codigo).FirstOrDefault();

                if (employee == null)
                {
                    continue;
                }

                var keyEmployee = listKeys.Where(k => k.Codigo == employee.Codigo).FirstOrDefault();
                var amount = employee.Salary;

                if (keyEmployee == null)
                {
                    continue;
                }

                if (mathOperation != null)
                {
                    switch (mathOperation)
                    {
                        case MathOperation.Multiplication:
                            amount = amount * multiplicator;
                            break;
                        case MathOperation.Division:
                            amount = amount / multiplicator;
                            break;
                        case MathOperation.Subtraction:
                            amount = amount - multiplicator;
                            break;
                        case MathOperation.Addition:
                            amount = amount + multiplicator;
                            break;
                        default:
                            break;
                    }
                }

                result.Add(new WorkedDayOffs()
                {
                    EmployeeCode = employee.Codigo,
                    EmployeeName = $"{employee.Name} {employee.LastName} {employee.MLastName}",
                    EmployeeSalary = employee.Salary,
                    Amount = amount,
                    Date = item.Date,
                    EmployeeActivity = keyEmployee.Tabulator.Activity ?? "",
                    Hours = 8,
                    NumConcept = dayoff.IncidentCodeItem!.ExternalCode,
                });
            }

            return result;
        }

        public byte[] ExecuteProcess(DownloadWorkedSunday downloadWorked)
        {
            var items = ExecuteProcess<GetWorkedSunday, IEnumerable<WorkedDayOffs>>(downloadWorked);

            if (downloadWorked.TypeFileDownload == TypeFileDownload.PDF)
            {
                var year = _globalPropertyService.YearOfOperation;
                var company = _companyRepository.GetById((decimal)downloadWorked.CompanyId);
                var period = _periodRepository.GetByFilter(p => p.Company == downloadWorked.CompanyId && p.TypePayroll == downloadWorked.PayrollId && p.NumPeriod == downloadWorked.NumberPeriod && p.Year == year).FirstOrDefault();
                var payroll = _payrollRepository.GetByFilter(p => p.Company == company!.Id && p.TypeNom == downloadWorked.PayrollId).First();

                var tenantName = "";
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    tenantName = _centerRepository.GetByFilter(c => c.Id.Trim() == downloadWorked.Tenant && c.Company == company!.Id).FirstOrDefault()?.DepartmentName ?? "";
                }
                else
                {
                    tenantName = _supervisorRepository.GetByFilter(s => s.Id == int.Parse(downloadWorked.Tenant!)).FirstOrDefault()?.Name ?? "";
                }

                return _pdfService.GeneratePDOM(items, company!.Name, tenantName,  $"{payroll.TypeNom} - {payroll.Label}", $"{period!.StartAdminDate} - {period!.ClosingAdminDate}", $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}");
            } else
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("WorkedSunday");
                    var index = 1;

                    worksheet.Cell($"A{index}").Value = "codigo";
                    worksheet.Cell($"B{index}").Value = "conc";
                    worksheet.Cell($"C{index}").Value = "importe";

                    index += 1;

                    worksheet.Column("A").Width = 8;
                    worksheet.Column("A").Style.NumberFormat.Format = "0";
                    worksheet.Column("B").Width = 4;
                    worksheet.Column("B").Style.NumberFormat.Format = "0";
                    worksheet.Column("C").Width = 10;
                    worksheet.Column("C").Style.NumberFormat.Format = "0.00";

                    foreach (var work in items)
                    {
                        worksheet.Cell($"A{index}").Value = work.EmployeeCode;
                        worksheet.Cell($"B{index}").Value = work.NumConcept;
                        worksheet.Cell($"C{index}").Value = Math.Round(work.Amount, 2);

                        index += 1;
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        return stream.ToArray();
                    }
                }
            }
        }
        public byte[] ExecuteProcess(DownloadWorkedDayoff downloadWorked)
        {
            var items = ExecuteProcess<GetWorkedDayOff, IEnumerable<WorkedDayOffs>>(downloadWorked);
            var configReport = _sysConfigService.ExecuteProcess<GetConfigReport, SysConfigReports>(new GetConfigReport
            {
                TypeConfigReport = TypeConfigReport.DayOffReport,
            });

            if (downloadWorked.TypeFileDownload == TypeFileDownload.PDF)
            {
                var year = _globalPropertyService.YearOfOperation;
                var company = _companyRepository.GetById((decimal)downloadWorked.CompanyId);
                var dayoff = _repository.GetById(Guid.Parse(downloadWorked.DayOffId));

                if (dayoff == null)
                {
                    throw new KeyNotFoundException("El registro seleccionado no existe");
                }

                var tenantName = "";
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    tenantName = _centerRepository.GetByFilter(c => c.Id.Trim() == downloadWorked.Tenant && c.Company == company!.Id).FirstOrDefault()?.DepartmentName ?? "";
                }
                else
                {
                    tenantName = _supervisorRepository.GetByFilter(s => s.Id == int.Parse(downloadWorked.Tenant!)).FirstOrDefault()?.Name ?? "";
                }

                return _pdfService.GenerateWorkedDayOff(items, company!.Name, tenantName, $"{dayoff.Date.ToString("dd MMMM")} - {dayoff.Description}", $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}");
            }
            else
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("WorkedSunday");
                    if (configReport.ConfigDayOffReport.TypeDayOffReport == TypeDayOffReport.New)
                    {
                        worksheet = MakeNewReportDayOff(worksheet, items);
                    } else
                    {
                        worksheet = MakeDefaultReportDayOff(worksheet, items);
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        return stream.ToArray();
                    }
                }
            }
        }

        public PagedResult<EmployeeDayOffOutput> ExecuteProcess(FilterEmployeesByPayroll filterEmployee)
        {
            List<Key> keys;
            var resultEmployee = new List<EmployeeDayOffOutput>();

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filterEmployee.CompanyId && 
                    item.TypeNom == filterEmployee.TypeNom && 
                    (filterEmployee.Tenant != "-999" ? item.Center.Trim() == filterEmployee.Tenant : true)
                ).ToList();
            }
            else
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filterEmployee.CompanyId && 
                    item.TypeNom == filterEmployee.TypeNom &&
                    (filterEmployee.Tenant != "-999" ? item.Supervisor == Convert.ToDecimal(filterEmployee.Tenant) : true)
                ).ToList();
            }

            var employeeCodes = keys.Select(k => k.Codigo).ToList();

            Func<Employee, bool> filter = employee => employee.Company == filterEmployee.CompanyId && employeeCodes.Contains(employee.Codigo);

            if (!string.IsNullOrWhiteSpace(filterEmployee.Search))
            { 
                var searchTerm = filterEmployee.Search.ToLower();
                filter = employee =>
                    employeeCodes.Contains(employee.Codigo) && employee.Company == filterEmployee.CompanyId &&
                    ($"{employee.Name} {employee.LastName} {employee.MLastName}".ToLower().Contains(searchTerm));
            }

            var pagedEmployees = _employeeRepository.GetWithPagination(filterEmployee.Page, filterEmployee.PageSize, filter);

            var pagedEmployeeCodes = pagedEmployees.Items.Select(e => e.Codigo).ToList();
            var assistanceIncidents = _assistanceIncident.GetContextEntity()
            .Include(ai => ai.ItemIncidentCode)
            .Where(ai =>
                pagedEmployeeCodes.Contains(ai.EmployeeCode) &&
                ai.CompanyId == filterEmployee.CompanyId &&
                ai.TimeOffRequest)
            .ToList();

            foreach (var item in pagedEmployees.Items)
            {
                resultEmployee.Add(new EmployeeDayOffOutput() {
                    Codigo = item.Codigo,
                    LastName = item.LastName,
                    MLastName = item.LastName,
                    Name = item.Name,
                    Salary = item.Salary,
                    AttendancesIncident = assistanceIncidents.Where(ai => ai.EmployeeCode == item.Codigo).ToList()
                });
            }

            return new PagedResult<EmployeeDayOffOutput>()
            {
                Items = resultEmployee,
                Page = pagedEmployees.Page,
                PageSize = pagedEmployees.PageSize,
                TotalPages = pagedEmployees.TotalPages,
                TotalRecords = pagedEmployees.TotalRecords,
            };
        }

        public EmployeeDayOffOutput ExecuteProcess(RegisterDaysOff registerDaysOff)
        {
            DateOnly cutoffDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-15);
            var findIncidentCode = _incidentCodeRepository.GetById(registerDaysOff.IncidentCode);
            var employeeId = registerDaysOff.EmployeeCode;

            var employee = _employeeRepository.GetByFilter(e => e.Company == registerDaysOff.CompanyId && e.Codigo == employeeId).FirstOrDefault();

            if (employee == null)
            {
                throw new BadHttpRequestException("El código de empleado no existe");
            }

            if (findIncidentCode == null)
            {
                throw new BadHttpRequestException("El código de incidencia no existe");
            }

            if (registerDaysOff.RequireAbsenceRequest)
            {
                _employeeAbsenRequestService.ExecuteProcess<RegisterDaysOff, bool>(registerDaysOff);
            }

            foreach (var date in registerDaysOff.Dates)
            {
                var existIncident = _assistanceIncident.GetByFilter(i => i.CompanyId == registerDaysOff.CompanyId && i.EmployeeCode == employeeId && i.Date == date).FirstOrDefault();

                if (existIncident != null)
                {
                    existIncident.IncidentCode = registerDaysOff.IncidentCode;
                    existIncident.UpdatedAt = DateTime.UtcNow;
                    existIncident.TimeOffRequest = true;

                    if (existIncident.IncidentCode != registerDaysOff.IncidentCode)
                    {
                        _auditLogRepository.Create(new AuditLog()
                        {
                            SectionCode = SectionCode.DayOff,
                            RecordId = existIncident.Id.ToString(),
                            Description = $"Se modificó un permiso para el empleado {existIncident.EmployeeCode} de la empresa {existIncident.CompanyId} el día {existIncident.Date}.",
                            OldValue = existIncident.IncidentCode,
                            NewValue = registerDaysOff.IncidentCode,
                            ByUserId = Guid.Parse(registerDaysOff.UserId!),
                        });
                    }
                } else
                {
                    var incident = _assistanceIncident.Create(new AssistanceIncident()
                    {
                        CompanyId = (int)registerDaysOff.CompanyId!,
                        EmployeeCode = (int)employeeId,
                        Date = date,
                        IncidentCode = registerDaysOff.IncidentCode,
                        TimeOffRequest = true,
                        Approved = !findIncidentCode.RequiredApproval,
                        ByUserId = Guid.Parse(registerDaysOff.UserId!)
                    });

                    _auditLogRepository.Create(new AuditLog()
                    {
                        SectionCode = SectionCode.DayOff,
                        RecordId = incident.Id.ToString(),
                        Description = $"Se agrego un permiso para el empleado {incident.EmployeeCode} de la empresa {incident.CompanyId} el día {incident.Date}.",
                        OldValue = registerDaysOff.IncidentCode,
                        NewValue = registerDaysOff.IncidentCode,
                        ByUserId = Guid.Parse(registerDaysOff.UserId!),
                    });
                }
            }

            _assistanceIncident.Save();
            _auditLogRepository.Save();

            var assistanceIncidents = _assistanceIncident.GetContextEntity()
            .Include(ai => ai.ItemIncidentCode)
            .Where(ai =>
                ai.EmployeeCode == employeeId &&
                ai.CompanyId == registerDaysOff.CompanyId &&
                ai.TimeOffRequest &&
                registerDaysOff.Dates.Contains(ai.Date))
            .ToList();

            return new EmployeeDayOffOutput()
            {
                AttendancesIncident = assistanceIncidents,
                Codigo = employee.Codigo,
                LastName = employee.LastName,
                MLastName = employee.LastName,
                Name = employee.Name,
                Salary = employee.Salary,
            };
        }

        public SyncIncapacityOutput ExecuteProcess(SyncIncapacity syncIncapacity)
        {
            var result = new SyncIncapacityOutput() { 
                Items = new List<EmployeeDayOffOutput>()
            };

            if (syncIncapacity.UserId == null)
            {
                throw new BadHttpRequestException("El usuario es requerido");
            }

            var period = _periodRepository.GetByFilter(p => p.Id == Guid.Parse(syncIncapacity.PeriodId)).FirstOrDefault();

            if (period == null)
            {
                throw new BadHttpRequestException("El periodo no existe");
            }

            var tenants = _userService.ExecuteProcess<GetTenantsUserByCompany, IEnumerable<string>>(new GetTenantsUserByCompany()
            {
                UserId = syncIncapacity.UserId,
                CompanyId = syncIncapacity.CompanyId,
            });


            if (tenants.Count() <= 0 || (syncIncapacity.TenantId != "-999" && tenants.Contains(syncIncapacity.TenantId)))
            {
                throw new BadHttpRequestException("El usuario no tiene acceso a este departamento/supervisor");
            }

            List<Key> keys;

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetByFilter(k => k.Company == syncIncapacity.CompanyId && k.TypeNom == syncIncapacity.TypeNom && syncIncapacity.TenantId == "-999" ? tenants.Contains(k.Center.Trim()) : k.Center.Trim() == syncIncapacity.TenantId.Trim()).ToList();
            }
            else
            {
                keys = _keyRepository.GetByFilter(k => k.Company == syncIncapacity.CompanyId && k.TypeNom == syncIncapacity.TypeNom && syncIncapacity.TenantId == "-999" ? tenants.Select(t => decimal.Parse(t)).ToList().Contains(k.Supervisor) : k.Supervisor == decimal.Parse(syncIncapacity.TenantId)).ToList();
            }
            var employees = keys.Select(k => k.Codigo).ToList();

            var listIncidentsCode = new List<int>() { 109, 110, 111 };
            var incidents = _deductionRepository.GetByFilter(
                d => listIncidentsCode.Contains(d.NumConc) &&
                d.YearOperation == period.Year && d.StartDate != null &&
                DateOnly.FromDateTime((DateTime)d.StartDate) >= period.StartDate &&
                DateOnly.FromDateTime((DateTime)d.StartDate) <= period.ClosingDate &&
                d.Company == syncIncapacity.CompanyId &&
                employees.Contains(d.Codigo)
            ).ToList();

            foreach (var incident in incidents)
            {
                if (incident.StartDate == null)
                {
                    continue;
                }

                var dates = new List<DateOnly>();
                var startDate = (DateTime)incident.StartDate;

                for (int i = 0; i < incident.Days; i++)
                {
                    dates.Add(DateOnly.FromDateTime(startDate.AddDays(i)));
                }

                var incidentCode = DefaultIncidentCodes.EG;
                if (incident.NumConc == 110)
                {
                    incidentCode = DefaultIncidentCodes.MT;
                }

                if (incident.NumConc == 111)
                {
                    incidentCode = DefaultIncidentCodes.RT;
                }

                var resultIncident = ExecuteProcess<RegisterDaysOff, EmployeeDayOffOutput>(new RegisterDaysOff()
                {
                    Dates = dates,
                    EmployeeCode = incident.Codigo,
                    IncidentCode = incidentCode,
                    CompanyId = incident.Company,
                    UserId = syncIncapacity.UserId,
                });
                result.TotalIncapacities++;

                ((List<EmployeeDayOffOutput>)result.Items).Add(resultIncident);
            }

            // sync vacations
            var kardexVacation = _kardexRepository.GetByFilter(
                k => k.Company == syncIncapacity.CompanyId &&
                employees.Contains(k.Codigo) &&
                k.NumConc == 30 && k.Paysheet == "VAC" && k.StartDate != null &&
                DateOnly.FromDateTime((DateTime)k.StartDate) >= period.StartDate &&
                DateOnly.FromDateTime((DateTime)k.StartDate) <= period.ClosingDate).ToList();

            foreach (var kvacation in kardexVacation)
            {
                if (kvacation.StartDate == null)
                {
                    continue;
                }

                var dates = new List<DateOnly>();
                var startDate = (DateTime)kvacation.StartDate;
                for (int i = 0; i < kvacation.Days; i++)
                {
                    dates.Add(DateOnly.FromDateTime(startDate.AddDays(i)));
                }

                var resultIncident = ExecuteProcess<RegisterDaysOff, EmployeeDayOffOutput>(new RegisterDaysOff()
                {
                    Dates = dates,
                    EmployeeCode = kvacation.Codigo,
                    IncidentCode = DefaultIncidentCodes.VAC,
                    CompanyId = kvacation.Company,
                    UserId = syncIncapacity.UserId,
                });
                result.TotalVacations++;
                ((List<EmployeeDayOffOutput>)result.Items).Add(resultIncident);
            }

            return result;
        }
    
        private IXLWorksheet MakeDefaultReportDayOff(IXLWorksheet worksheet, IEnumerable<WorkedDayOffs> items)
        {
            var index = 1;

            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "conc";
            worksheet.Cell($"C{index}").Value = "importe";
            worksheet.Cell($"D{index}").Value = "fecha";
            worksheet.Cell($"E{index}").Value = "horas";

            index++;

            worksheet.Column("A").Width = 8;
            worksheet.Column("A").Style.NumberFormat.Format = "0";
            worksheet.Column("B").Width = 4;
            worksheet.Column("B").Style.NumberFormat.Format = "0";
            worksheet.Column("C").Width = 10;
            worksheet.Column("C").Style.NumberFormat.Format = "0.00";
            worksheet.Column("D").Width = 10;
            worksheet.Column("D").Style.NumberFormat.Format = "dd/mm/yyyyy";
            worksheet.Column("E").Width = 6;
            worksheet.Column("E").Style.NumberFormat.Format = "0.00";

            foreach (var work in items)
            {
                worksheet.Cell($"A{index}").Value = work.EmployeeCode;
                worksheet.Cell($"B{index}").Value = work.NumConcept;
                worksheet.Cell($"C{index}").Value = work.Amount;
                worksheet.Cell($"D{index}").Value = work.Date.ToString("dd/MM/yyyy");
                worksheet.Cell($"E{index}").Value = 8;

                index += 1;
            }

            return worksheet;
        }

        private IXLWorksheet MakeNewReportDayOff(IXLWorksheet worksheet, IEnumerable<WorkedDayOffs> items)
        {
            var index = 1;

            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "Conc";
            worksheet.Cell($"C{index}").Value = "Importe";

            index++;

            worksheet.Column("A").Width = 8;
            worksheet.Column("A").Style.NumberFormat.Format = "0";
            worksheet.Column("B").Width = 4;
            worksheet.Column("B").Style.NumberFormat.Format = "0";
            worksheet.Column("C").Width = 10;
            worksheet.Column("C").Style.NumberFormat.Format = "0.00";

            foreach (var work in items)
            {
                worksheet.Cell($"A{index}").Value = work.EmployeeCode;
                worksheet.Cell($"B{index}").Value = work.NumConcept;
                worksheet.Cell($"C{index}").Value = work.Amount;

                index += 1;
            }

            return worksheet;
        }
    }
}
