using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Services.Utilities;

namespace PrenominaApi.Services
{
    public class AttendanceRecordsService : Service<AttendanceRecords>
    {
        private readonly IBaseService<Employee> _employeeService;
        private readonly IBaseRepository<Payroll> _payrollRepository;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Company> _companiesRepository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRepository;
        private readonly IBaseRepositoryPrenomina<SystemConfig> _systemConfigRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncident> _assistanceIncident;
        private readonly IBaseRepositoryPrenomina<IncidentCode> _incidentCodeRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeCheckIns> _employeeCheckIns;
        private readonly IBaseRepositoryPrenomina<PeriodStatus> _perioStatusRepository;
        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly PDFService _pdfService;

        public AttendanceRecordsService(
            IBaseRepository<AttendanceRecords> repository,
            IBaseRepository<Payroll> payrollRepository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Company> companiesRepository,
            IBaseRepository<Center> centerRepository,
            IBaseRepository<Supervisor> supervisorRepository,
            IBaseService<Employee> employeeService,
            IBaseRepositoryPrenomina<SystemConfig> systemConfigRepository,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncident,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<EmployeeCheckIns> employeeCheckIns,
            IBaseRepositoryPrenomina<PeriodStatus> perioStatusRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodRepository,
            GlobalPropertyService globalPropertyService,
            PDFService pdfService
        ) : base(repository) {
            _employeeService = employeeService;
            _payrollRepository = payrollRepository;
            _periodRepository = periodRepository;
            _keyRepository = keyRepository;
            _companiesRepository = companiesRepository;
            _centerRepository = centerRepository;
            _supervisorRepository = supervisorRepository;
            _systemConfigRepository = systemConfigRepository;
            _assistanceIncident = assistanceIncident;
            _incidentCodeRepository = incidentCodeRepository;
            _employeeCheckIns = employeeCheckIns;
            _globalPropertyService = globalPropertyService;
            _perioStatusRepository = perioStatusRepository;
            _pdfService = pdfService;
        }

        public PagedResult<EmployeeAttendancesOutput> ExecuteProcess(GetAttendanceEmployees filter)
        {
            List<Key> keys;

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filter.Company && 
                    item.TypeNom == filter.TypeNomina && 
                    (filter.Tenant != "-999" ? item.Center.Trim() == filter.Tenant : true)
                ).Include(k => k.Tabulator).ToList();
            } else
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filter.Company &&
                    item.TypeNom == filter.TypeNomina &&
                    (filter.Tenant != "-999" ? item.Supervisor == Convert.ToDecimal(filter.Tenant) : true)
                ).Include(k => k.Tabulator).ToList();
            }

            var employeeCodes = keys.Select(k => k.Codigo).ToList();
            var year = _globalPropertyService.YearOfOperation;
            var periodDates = _periodRepository.GetByFilter((period) => period.TypePayroll == filter.TypeNomina && period.Company == filter.Company && period.NumPeriod == filter.NumPeriod && period.Year == year).FirstOrDefault();

            if (periodDates is null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            var assistanceIncidents = _assistanceIncident.GetContextEntity().Include(ai => ai.ItemIncidentCode).Where(ai => employeeCodes.Contains(ai.EmployeeCode) && ai.CompanyId == filter.Company && ai.Date >= periodDates.StartDate && ai.Date <= periodDates.ClosingDate).Include(ai => ai.ItemIncidentCode).ToList();

            var lowerSearch = filter.Search?.ToLower();
            PagedResult<Employee> employees = _employeeService.GetWithPagination(
                filter.Paginator.Page,
                filter.Paginator.PageSize,
                item => employeeCodes.Contains(item.Codigo) && item.Company == filter.Company && item.Active == 'S' && (
                    string.IsNullOrWhiteSpace(lowerSearch) || item.Codigo.ToString().Contains(lowerSearch) || (item.Name + " " + item.LastName + " " + item.MLastName).ToLower().Contains(lowerSearch)
                )
            );
            List<EmployeeCheckIns> attendances = _employeeCheckIns.GetByFilter(
                item => item.CheckIn != TimeOnly.MinValue && item.Date >= periodDates.StartDate && item.Date <= periodDates.ClosingDate && employees.Items.Any(e => e.Codigo == item.EmployeeCode && e.Company == item.CompanyId)
            ).ToList();

            foreach (var incident in assistanceIncidents)
            {
                attendances.Add(new EmployeeCheckIns()
                {
                    CheckIn = TimeOnly.MinValue,
                    EmployeeCode = incident.EmployeeCode,
                    CompanyId = incident.CompanyId,
                    Date = incident.Date,
                });
            }

            var result = employees.Items.Select(employee => new EmployeeAttendancesOutput
            {
                Codigo = employee.Codigo,
                LastName = employee.LastName,
                MLastName = employee.MLastName,
                Name = employee.Name,
                Company = employee.Company,
                Salary = employee.Salary,
                Activity = keys.FirstOrDefault(k => k.Codigo == employee.Codigo)?.Tabulator.Activity ?? "",
                Attendances = attendances.Where(attendace => attendace.EmployeeCode == employee.Codigo && attendace.CompanyId == employee.Company)
                .GroupBy(attendance => attendance.Date)
                .Select(check =>
                {
                    var orderedChecks = check.Where(x => x.CheckIn != TimeOnly.MinValue).OrderBy(x => x.CheckIn).ToList();
                    var employeeAssistanceIncidents = assistanceIncidents.Where(ai => ai.EmployeeCode == employee.Codigo && ai.Date == check.Key);
                    var defaultIncidentCode = employeeAssistanceIncidents.Where(ai => ai.ItemIncidentCode?.IsAdditional == false && ai.Approved).FirstOrDefault();

                    return new AttendanceOutput
                    {
                        Date = check.Key,
                        IncidentCode = defaultIncidentCode == null ? "N/A" : defaultIncidentCode.IncidentCode,
                        IncidentCodeLabel = defaultIncidentCode?.ItemIncidentCode?.Label ?? "",
                        TypeNom = check.First().TypeNom,
                        CheckEntry = orderedChecks.Where(x => x.EoS == EntryOrExit.Entry).MinBy(x => x.CheckIn)?.CheckIn.ToString("HH:mm:ss"),
                        CheckOut = orderedChecks.Where(x => x.EoS == EntryOrExit.Exit).MaxBy(x => x.CheckIn)?.CheckIn.ToString("HH:mm:ss"),
                        AssistanceIncidents = employeeAssistanceIncidents.Select(ai =>
                        {
                            return new AssistanceIncidentOutput()
                            {
                                Id = ai.Id,
                                Date = ai.Date,
                                IncidentCode = ai.IncidentCode,
                                Approved = ai.Approved,
                                IsAdditional = ai.ItemIncidentCode?.IsAdditional ?? false,
                                Label = ai.ItemIncidentCode?.Label ?? "",
                                TimeOffRequest = ai.TimeOffRequest,
                                UpdatedAt = ai.UpdatedAt
                            };
                        }).ToList()
                    };
                }).ToList()
            }).ToList();

            return new PagedResult<EmployeeAttendancesOutput>
            {
                Items = result,
                Page = employees.Page,
                PageSize = employees.PageSize,
                TotalPages = employees.TotalPages,
                TotalRecords = employees.TotalRecords
            };
        }

        public InitAttendanceRecords ExecuteProcess(int companyId)
        {
            var year = _globalPropertyService.YearOfOperation;

            var payrolls = _payrollRepository.GetByFilter((payroll) => payroll.Company == companyId);
            var periods = _periodRepository.GetByFilter((period) => period.Company == companyId && period.Year == year).OrderBy(p => p.NumPeriod);
            var incidentCodes = _incidentCodeRepository.GetAll();
            var periodStatus = _perioStatusRepository.GetAll();

            return new InitAttendanceRecords() {
                Payrolls = payrolls,
                Periods = periods,
                IncidentCodes = incidentCodes,
                PeriodStatus = periodStatus,
            };
        }

        public byte[] ExecuteProcess(DownloadAttendanceEmployee downloadAttendance)
        {
            IQueryable<Key> keyQuery = _keyRepository.GetContextEntity().Include(k => k.Tabulator).Where(k => k.Company == downloadAttendance.Company && k.TypeNom == downloadAttendance.TypeNomina);
            var company = _companiesRepository.GetById(downloadAttendance.Company);

            if (downloadAttendance.Tenant != "-999")
            {
                keyQuery = _globalPropertyService.TypeTenant == TypeTenant.Department ? keyQuery.Where(k => k.Center.Trim() == downloadAttendance.Tenant) : keyQuery.Where(k => k.Supervisor == Convert.ToDecimal(downloadAttendance.Tenant));
            }

            var keys = keyQuery.ToList();
            var employeeCodes = keys.Select(k => k.Codigo).ToHashSet();
            var year = _globalPropertyService.YearOfOperation;
            var period = _periodRepository.GetByFilter(p => p.TypePayroll == downloadAttendance.TypeNomina &&
                p.Company == downloadAttendance.Company &&
                p.NumPeriod == downloadAttendance.NumPeriod &&
                p.Year == year
            ).FirstOrDefault();
            var payroll = _payrollRepository.GetByFilter(p => p.Company == company!.Id && p.TypeNom == downloadAttendance.TypeNomina).First();

            if (period == null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            var tenantName = "";
            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                tenantName = _centerRepository.GetByFilter(c => c.Id.Trim() == downloadAttendance.Tenant && c.Company == company!.Id).FirstOrDefault()?.DepartmentName ?? "";
            }
            else
            {
                tenantName = _supervisorRepository.GetByFilter(s => s.Id == int.Parse(downloadAttendance.Tenant!)).FirstOrDefault()?.Name ?? "";
            }

            var assistanceIncidents = _assistanceIncident.GetContextEntity().Include(ai => ai.ItemIncidentCode).Where(ai =>
                employeeCodes.Contains(ai.EmployeeCode) &&
                ai.CompanyId == downloadAttendance.Company &&
                ai.Date >= period.StartDate &&
                ai.Date <= period.ClosingDate
            ).ToList();

            var employees = _employeeService.GetByFilter(e => employeeCodes.Contains(e.Codigo) && e.Company == downloadAttendance.Company && e.Active == 'S').ToList();
            var employeeLookup = employees.ToDictionary(e => (e.Codigo, e.Company));
            var codesToFilter = employees.Select(e => e.Codigo).ToHashSet();
            var attendaces = _employeeCheckIns.GetByFilter(a =>
                a.CheckIn != TimeOnly.MinValue &&
                a.Date >= period.StartDate &&
                a.Date <= period.ClosingDate &&
                codesToFilter.Contains(a.EmployeeCode) &&
                a.CompanyId == downloadAttendance.Company
            ).ToList();

            attendaces.AddRange(assistanceIncidents.Select(ai => new EmployeeCheckIns()
            {
                CheckIn = TimeOnly.MinValue,
                EmployeeCode = ai.EmployeeCode,
                CompanyId = ai.CompanyId,
                Date = ai.Date
            }));

            var employeeAttendancesResult = employees.Select(emp =>
            {
                var activity = keys.FirstOrDefault(k => k.Codigo == emp.Codigo)?.Tabulator.Activity ?? "";
                var empAttendaces = attendaces.Where(a => a.EmployeeCode == emp.Codigo && a.CompanyId == emp.Company).GroupBy(a => a.Date).Select(group =>
                {
                    //var checks = group.Where(g => TimeSpan.TryParse(g.CheckInOut, out _)).OrderBy(g => TimeSpan.Parse(g.CheckInOut ?? "00:00:00")).ToList();
                    var checks = group.Where(g => g.CheckIn != TimeOnly.MinValue).OrderBy(g => g.CheckIn).ToList();

                    var empIncidents = assistanceIncidents.Where(ai => ai.EmployeeCode == emp.Codigo && ai.Date == group.Key);

                    var defaultIncident = empIncidents.FirstOrDefault(ai => ai.ItemIncidentCode?.IsAdditional == false && ai.Approved);

                    return new AttendanceOutput
                    {
                        Date = group.Key,
                        IncidentCode = defaultIncident?.IncidentCode ?? "N/A",
                        IncidentCodeLabel = defaultIncident?.ItemIncidentCode?.Label ?? "",
                        TypeNom = group.First().TypeNom,
                        //CheckEntry = checks.FirstOrDefault(c => c.TypeInOut?.Contains("E") == true || c.TypeInOut?.Contains("1") == true)?.CheckInOut,
                        //CheckOut = checks.LastOrDefault(c => c.TypeInOut?.Contains("S") == true || c.TypeInOut?.Contains("2") == true)?.CheckInOut,
                        CheckEntry = checks.FirstOrDefault(c => c.EoS == EntryOrExit.Entry)?.CheckIn.ToString("HH:mm:ss"),
                        CheckOut = checks.LastOrDefault(c => c.EoS == EntryOrExit.Exit)?.CheckIn.ToString("HH:mm:ss"),
                        AssistanceIncidents = empIncidents.Select(ai => new AssistanceIncidentOutput
                        {
                            Id = ai.Id,
                            Date = ai.Date,
                            IncidentCode = ai.IncidentCode,
                            Approved = ai.Approved,
                            IsAdditional = ai.ItemIncidentCode?.IsAdditional ?? false,
                            Label = ai.ItemIncidentCode?.Label ?? "",
                            TimeOffRequest = ai.TimeOffRequest,
                            UpdatedAt = ai.UpdatedAt
                        }).ToList()
                    };
                }).ToList();

                return new EmployeeAttendancesOutput
                {
                    Codigo = emp.Codigo,
                    LastName = emp.LastName,
                    MLastName = emp.MLastName,
                    Name = emp.Name,
                    Company = emp.Company,
                    Salary = emp.Salary,
                    Activity = activity,
                    Attendances = empAttendaces,
                };
            }).ToList();

            if (downloadAttendance.TypeFileDownload == TypeFileDownload.PDF)
            {
                List<DateOnly> listDates = DateService.GetListDate(period.StartDate, period.ClosingDate);
                return _pdfService.GenerateAttendance(employeeAttendancesResult, company?.Name ?? "", tenantName, $"{period.StartDate} - {period.ClosingDate}", listDates, $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}", $"{payroll.TypeNom} - {payroll.Label}");
            } else
            {
                List<DateOnly> listDates = DateService.GetListDate(period.StartDate, period.ClosingDate);
                List<DateOnly> listAdminsDates = DateService.GetListDate(period.StartAdminDate, period.ClosingAdminDate);

                var incidentsApply = SysConfig.IncidentApplyToAttendance;
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("T. Asistencia");
                    var index = 1;

                    worksheet.Cell($"A{index}").Value = "Codigo";
                    worksheet.Cell($"B{index}").Value = "conc";
                    worksheet.Cell($"C{index}").Value = "importe";
                    worksheet.Cell($"D{index}").Value = "fecha";
                    worksheet.Cell($"E{index}").Value = "tipo";
                    worksheet.Cell($"F{index}").Value = "dias";

                    index++;

                    worksheet.Column("A").Width = 8;
                    worksheet.Column("A").Style.NumberFormat.Format = "0";
                    worksheet.Column("B").Width = 4;
                    worksheet.Column("B").Style.NumberFormat.Format = "0";
                    worksheet.Column("C").Width = 10;
                    worksheet.Column("C").Style.NumberFormat.Format = "0.00";
                    worksheet.Column("D").Width = 10;
                    worksheet.Column("D").Style.NumberFormat.Format = "dd/mm/yyyy";
                    worksheet.Column("E").Width = 8;
                    worksheet.Column("E").Style.NumberFormat.Format = "0";
                    worksheet.Column("F").Width = 6;
                    worksheet.Column("F").Style.NumberFormat.Format = "0.00";

                    foreach (var employee in employeeAttendancesResult)
                    {
                        List<AttendanceOutput> onlyIncidentApply = employee.Attendances!.Where(a => incidentsApply.Contains(a.IncidentCode)).ToList();

                        foreach (var attendance in onlyIncidentApply)
                        {
                            var indexDate = listDates.IndexOf(attendance.Date);
                            var findAdminDate = listAdminsDates[indexDate];

                            if (indexDate != -1)
                            {
                                worksheet.Cell($"A{index}").Value = employee.Codigo;
                                worksheet.Cell($"B{index}").Value = 108;
                                worksheet.Cell($"C{index}").Value = employee.Salary;
                                worksheet.Cell($"D{index}").Value = findAdminDate.ToString("dd/MM/yyyy");
                                worksheet.Cell($"E{index}").Value = attendance.IncidentCode;
                                worksheet.Cell($"F{index}").Value = 1;

                                index++;
                            }
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        return stream.ToArray();
                    }
                }
            }
        }
    }
}
