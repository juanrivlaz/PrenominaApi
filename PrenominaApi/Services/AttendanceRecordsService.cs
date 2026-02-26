using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Attendance;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Attendance;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Services.Utilities;
using PrenominaApi.Services.Utilities.AdditionalPayPdf;
using PrenominaApi.Services.Utilities.AttendancePdf;

namespace PrenominaApi.Services
{
    public class AttendanceRecordsService : Service<AttendanceRecords>
    {
        private readonly IBaseService<Employee> _employeeService;
        private readonly IBaseRepository<Payroll> _payrollRepository;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Models.Company> _companiesRepository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRepository;
        private readonly IBaseRepository<Employee> _employeeRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncident> _assistanceIncident;
        private readonly IBaseRepositoryPrenomina<IncidentCode> _incidentCodeRepository;
        private readonly IBaseRepositoryPrenomina<EmployeeCheckIns> _employeeCheckIns;
        private readonly IBaseRepositoryPrenomina<PeriodStatus> _perioStatusRepository;
        private readonly IBaseRepositoryPrenomina<User> _userRepository;
        private readonly IBaseServicePrenomina<SystemConfig> _sysConfigService;
        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly PDFService _pdfService;
        private readonly AttendancePdfService _attendancePdfService;
        private readonly AdditionalPayPdfService _additionalPayPdfService;
        private readonly ICacheService _cacheService;

        public AttendanceRecordsService(
            IBaseRepository<AttendanceRecords> repository,
            IBaseRepository<Payroll> payrollRepository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Models.Company> companiesRepository,
            IBaseRepository<Center> centerRepository,
            IBaseRepository<Supervisor> supervisorRepository,
            IBaseRepository<Employee> employeeRepository,
            IBaseService<Employee> employeeService,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncident,
            IBaseRepositoryPrenomina<IncidentCode> incidentCodeRepository,
            IBaseRepositoryPrenomina<EmployeeCheckIns> employeeCheckIns,
            IBaseRepositoryPrenomina<PeriodStatus> perioStatusRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseServicePrenomina<SystemConfig> sysConfigService,
            IBaseServicePrenomina<Models.Prenomina.Period> periodRepository,
            GlobalPropertyService globalPropertyService,
            PDFService pdfService,
            AttendancePdfService attendancePdfService,
            AdditionalPayPdfService additionalPayPdfService,
            ICacheService cacheService
        ) : base(repository)
        {
            _employeeService = employeeService;
            _payrollRepository = payrollRepository;
            _periodRepository = periodRepository;
            _keyRepository = keyRepository;
            _companiesRepository = companiesRepository;
            _centerRepository = centerRepository;
            _supervisorRepository = supervisorRepository;
            _employeeRepository = employeeRepository;
            _assistanceIncident = assistanceIncident;
            _incidentCodeRepository = incidentCodeRepository;
            _employeeCheckIns = employeeCheckIns;
            _globalPropertyService = globalPropertyService;
            _perioStatusRepository = perioStatusRepository;
            _userRepository = userRepository;
            _pdfService = pdfService;
            _attendancePdfService = attendancePdfService;
            _additionalPayPdfService = additionalPayPdfService;
            _sysConfigService = sysConfigService;
            _cacheService = cacheService;
        }

        public PagedResult<EmployeeAttendancesOutput> ExecuteProcess(GetAttendanceEmployees filter)
        {
            // Construir query base para keys con filtros
            var keysQuery = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(item => item.Company == filter.Company && item.TypeNom == filter.TypeNomina);

            // Aplicar filtro de tenant
            if (filter.Tenant != "-999")
            {
                keysQuery = _globalPropertyService.TypeTenant == TypeTenant.Department
                    ? keysQuery.Where(item => item.Center.Trim() == filter.Tenant)
                    : keysQuery.Where(item => item.Supervisor == Convert.ToDecimal(filter.Tenant));
            }

            // Proyectar solo lo necesario - evita cargar entidades completas
            var keysData = keysQuery
                .Include(k => k.Tabulator)
                .Select(k => new { k.Codigo, Activity = k.Tabulator.Activity ?? "" })
                .ToList();

            // Usar HashSet para búsquedas O(1)
            var employeeCodes = keysData.Select(k => k.Codigo).ToHashSet();
            var activityLookup = keysData.ToDictionary(k => k.Codigo, k => k.Activity);

            var year = _globalPropertyService.YearOfOperation;

            // Obtener período con AsNoTracking
            var periodDates = _periodRepository.GetByFilter(
                period => period.TypePayroll == filter.TypeNomina &&
                          period.Company == filter.Company &&
                          period.NumPeriod == filter.NumPeriod &&
                          period.Year == year
            ).AsQueryable().AsNoTracking().FirstOrDefault();

            if (periodDates is null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            // Obtener incidentes en una sola query optimizada
            var assistanceIncidents = _assistanceIncident.GetContextEntity()
                .AsNoTracking()
                .Where(ai => employeeCodes.Contains(ai.EmployeeCode) &&
                             ai.CompanyId == filter.Company &&
                             ai.Date >= periodDates.StartDate &&
                             ai.Date <= periodDates.ClosingDate)
                .Select(ai => new
                {
                    ai.Id,
                    ai.EmployeeCode,
                    ai.CompanyId,
                    ai.Date,
                    ai.IncidentCode,
                    ai.Approved,
                    ai.TimeOffRequest,
                    ai.UpdatedAt,
                    IsAdditional = ai.ItemIncidentCode != null && ai.ItemIncidentCode.IsAdditional,
                    Label = ai.ItemIncidentCode != null ? ai.ItemIncidentCode.Label : ""
                })
                .ToList();

            // Indexar incidentes por empleado y fecha para O(1) lookup
            var incidentsLookup = assistanceIncidents
                .GroupBy(ai => (ai.EmployeeCode, ai.Date))
                .ToDictionary(g => g.Key, g => g.ToList());

            var lowerSearch = filter.Search?.ToLower();

            // Paginación de empleados
            var employees = _employeeService.GetWithPagination(
                filter.Paginator.Page,
                filter.Paginator.PageSize,
                item => employeeCodes.Contains(item.Codigo) &&
                        item.Company == filter.Company &&
                        item.Active == 'S' &&
                        (string.IsNullOrWhiteSpace(lowerSearch) ||
                         item.Codigo.ToString().Contains(lowerSearch) ||
                         (item.Name + " " + item.LastName + " " + item.MLastName).ToLower().Contains(lowerSearch))
            );

            // HashSet de empleados paginados para filtrar checkins
            var paginatedEmployeeCodes = employees.Items.Select(e => e.Codigo).ToHashSet();

            // Obtener checkins optimizado
            var attendances = _employeeCheckIns.GetContextEntity()
                .AsNoTracking()
                .Where(item => item.CheckIn != TimeOnly.MinValue &&
                               item.Date >= periodDates.StartDate &&
                               item.Date <= periodDates.ClosingDate &&
                               paginatedEmployeeCodes.Contains(item.EmployeeCode) &&
                               item.CompanyId == filter.Company)
                .Select(item => new
                {
                    item.Id,
                    item.EmployeeCode,
                    item.CompanyId,
                    item.Date,
                    item.CheckIn,
                    item.EoS,
                    item.TypeNom
                })
                .ToList();

            // Indexar checkins por empleado y fecha
            var checkinsLookup = attendances
                .GroupBy(a => (a.EmployeeCode, a.Date))
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CheckIn).ToList());

            // Obtener fechas del período
            var allDates = DateService.GetListDate(periodDates.StartDate, periodDates.ClosingDate);

            // Procesar resultados
            var result = employees.Items.Select(employee =>
            {
                var employeeAttendances = new List<AttendanceOutput>();

                foreach (var date in allDates)
                {
                    var key = (employee.Codigo, date);

                    // Obtener checkins para esta fecha
                    var dayCheckins = checkinsLookup.TryGetValue(key, out var checks) ? checks : null;

                    // Obtener incidentes para esta fecha
                    var dayIncidents = incidentsLookup.TryGetValue(key, out var incidents)
                        ? incidents
                        : new List<dynamic>();

                    var defaultIncident = dayIncidents
                        .Where(ai => !ai.IsAdditional && ai.Approved)
                        .FirstOrDefault();

                    var checkEntry = dayCheckins?.Where(x => x.EoS == EntryOrExit.Entry).MinBy(x => x.CheckIn);
                    var checkOut = dayCheckins?.Where(x => x.EoS == EntryOrExit.Exit).MaxBy(x => x.CheckIn);

                    employeeAttendances.Add(new AttendanceOutput
                    {
                        Date = date,
                        IncidentCode = defaultIncident?.IncidentCode ?? "N/A",
                        IncidentCodeLabel = defaultIncident?.Label ?? "",
                        TypeNom = dayCheckins?.FirstOrDefault()?.TypeNom ?? 0,
                        CheckEntryId = checkEntry?.Id,
                        CheckEntry = checkEntry?.CheckIn.ToString("HH:mm:ss"),
                        CheckOutId = checkOut?.Id,
                        CheckOut = checkOut?.CheckIn.ToString("HH:mm:ss"),
                        AssistanceIncidents = dayIncidents.Select(ai => new AssistanceIncidentOutput
                        {
                            Id = ai.Id,
                            Date = ai.Date,
                            IncidentCode = ai.IncidentCode,
                            Approved = ai.Approved,
                            IsAdditional = ai.IsAdditional,
                            Label = ai.Label,
                            TimeOffRequest = ai.TimeOffRequest,
                            UpdatedAt = ai.UpdatedAt
                        }).ToList()
                    });
                }

                return new EmployeeAttendancesOutput
                {
                    Codigo = employee.Codigo,
                    LastName = employee.LastName,
                    MLastName = employee.MLastName,
                    Name = employee.Name,
                    Company = employee.Company,
                    Salary = employee.Salary,
                    Activity = activityLookup.TryGetValue(employee.Codigo, out var activity) ? activity : "",
                    Attendances = employeeAttendances
                };
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
            if (string.IsNullOrEmpty(_globalPropertyService.UserId))
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            var year = _globalPropertyService.YearOfOperation;

            // Obtener usuario con rol en una sola query
            var user = _userRepository.GetContextEntity()
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Id == Guid.Parse(_globalPropertyService.UserId))
                .Select(u => new { u.Id, u.RoleId, RoleCode = u.Role!.Code })
                .FirstOrDefault();

            if (user == null)
            {
                throw new BadHttpRequestException("Unauthorized");
            }

            // Usar caché para payrolls
            var payrolls = _cacheService.GetOrCreate(
                CacheKeys.GetPayrollsKey(companyId),
                () => _payrollRepository.GetByFilter(p => p.Company == companyId).ToList(),
                TimeSpan.FromMinutes(30)
            );

            // Períodos con caché
            var periodsQuery = _periodRepository.GetByFilter(
                period => period.Company == companyId && period.Year == year
            );

            var periods = user.RoleCode == RoleCode.Sudo
                ? periodsQuery.OrderBy(p => p.NumPeriod).ToList()
                : periodsQuery.Where(p => p.IsActive).OrderBy(p => p.NumPeriod).ToList();

            // Incident codes con caché y proyección
            var incidentCodes = _cacheService.GetOrCreate(
                CacheKeys.IncidentCodes,
                () => _incidentCodeRepository.GetContextEntity()
                    .AsNoTracking()
                    .Include(ic => ic.IncidentCodeAllowedRoles)
                    .ToList(),
                TimeSpan.FromMinutes(60)
            );

            // Filtrar por rol si es necesario
            var filteredIncidentCodes = user.RoleCode == RoleCode.Sudo
                ? incidentCodes
                : incidentCodes.Where(ic =>
                    !ic.RestrictedWithRoles ||
                    (ic.IncidentCodeAllowedRoles != null &&
                     ic.IncidentCodeAllowedRoles.Any(ar => ar.RoleId == user.RoleId))
                ).ToList();

            // Period status con caché
            var periodStatus = _cacheService.GetOrCreate(
                CacheKeys.PeriodStatus,
                () => _perioStatusRepository.GetAll().ToList(),
                TimeSpan.FromMinutes(5)
            );

            return new InitAttendanceRecords()
            {
                Payrolls = payrolls,
                Periods = periods,
                IncidentCodes = filteredIncidentCodes,
                PeriodStatus = periodStatus,
            };
        }

        public IEnumerable<AdditionalPay> ExecuteProcess(GetAdditionalPay getAdditionalPay)
        {
            // Construir query de keys
            var queryKey = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(k => k.Company == getAdditionalPay.Company && k.TypeNom == getAdditionalPay.TypeNomina);

            if (getAdditionalPay.Tenant != "-999")
            {
                queryKey = _globalPropertyService.TypeTenant == TypeTenant.Department
                    ? queryKey.Where(k => k.Center.Trim() == getAdditionalPay.Tenant)
                    : queryKey.Where(k => k.Supervisor == Convert.ToDecimal(getAdditionalPay.Tenant));
            }

            var keysData = queryKey
                .Include(k => k.Tabulator)
                .Select(k => new { k.Codigo, Activity = k.Tabulator.Activity ?? "" })
                .ToList();

            var employeeCodes = keysData.Select(k => k.Codigo).ToHashSet();
            var activityLookup = keysData.ToDictionary(k => k.Codigo, k => k.Activity);

            var year = _globalPropertyService.YearOfOperation;
            var periodDates = _periodRepository.GetByFilter(
                period => period.TypePayroll == getAdditionalPay.TypeNomina &&
                          period.Company == getAdditionalPay.Company &&
                          period.NumPeriod == getAdditionalPay.NumPeriod &&
                          period.Year == year
            ).AsQueryable().AsNoTracking().SingleOrDefault();

            if (periodDates is null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            // Obtener códigos de incidencia adicionales con operación
            var incidentCodes = _incidentCodeRepository.GetContextEntity()
                .AsNoTracking()
                .Where(ic => ic.IsAdditional && ic.WithOperation)
                .Select(ic => ic.Code)
                .ToHashSet();

            // Query optimizada de incidentes
            var assistanceIncidents = _assistanceIncident.GetContextEntity()
                .AsNoTracking()
                .Where(ai => employeeCodes.Contains(ai.EmployeeCode) &&
                             ai.CompanyId == getAdditionalPay.Company &&
                             ai.Date >= periodDates.StartDate &&
                             ai.Date <= periodDates.ClosingDate &&
                             incidentCodes.Contains(ai.IncidentCode) &&
                             ai.MetaIncidentCodeJson != null)
                .Include(ai => ai.ItemIncidentCode)
                    .ThenInclude(ic => ic!.IncidentCodeMetadata)
                .ToList();

            // Obtener empleados en una sola query
            var employeesDict = _employeeRepository.GetContextEntity()
                .AsNoTracking()
                .Where(item => employeeCodes.Contains(item.Codigo) &&
                               item.Company == getAdditionalPay.Company &&
                               item.Active == 'S')
                .ToDictionary(e => (e.Codigo, e.Company));

            return assistanceIncidents.Select(incident =>
            {
                var columnForOperation = incident.ItemIncidentCode?.IncidentCodeMetadata!.ColumnForOperation;

                if (!employeesDict.TryGetValue((incident.EmployeeCode, incident.CompanyId), out var employee))
                {
                    return null;
                }

                var baseValue = columnForOperation == ColumnForOperation.Salary
                    ? employee.Salary
                    : incident.MetaIncidentCode!.BaseValue;
                var operationValue = incident.MetaIncidentCode!.OperationValue;

                var (total, operatorSymbol, operatorText) = CalculateOperation(
                    incident.ItemIncidentCode!.IncidentCodeMetadata!.MathOperation,
                    baseValue,
                    operationValue
                );

                return new AdditionalPay
                {
                    EmployeeName = $"{employee.Name} {employee.LastName} {employee.MLastName}",
                    EmployeeCode = employee.Codigo,
                    EmployeeActivity = activityLookup.TryGetValue(employee.Codigo, out var activity) ? activity : "",
                    Company = "",
                    Date = incident.Date,
                    IncidentCode = incident.IncidentCode,
                    Column = columnForOperation == ColumnForOperation.Salary ? "Empleado:Salario" : "Custom",
                    BaseValue = baseValue,
                    Operator = operatorSymbol,
                    OperatorText = operatorText,
                    OperationValue = operationValue,
                    Total = total
                };
            }).Where(x => x != null).Cast<AdditionalPay>();
        }

        private static (decimal total, string symbol, string text) CalculateOperation(
            MathOperation operation, decimal baseValue, decimal operationValue)
        {
            return operation switch
            {
                MathOperation.Addition => (baseValue + operationValue, "add", "Suma"),
                MathOperation.Subtraction => (baseValue - operationValue, "remove", "Resta"),
                MathOperation.Multiplication => (baseValue * operationValue, "close", "Multiplicación"),
                MathOperation.Division when operationValue != 0 => (baseValue / operationValue, "open_size_2", "División"),
                _ => (baseValue, "", "")
            };
        }

        public byte[] ExecuteProcess(DownloadAdditionalPay downloadAdditionalPay)
        {
            var company = _companiesRepository.GetById(downloadAdditionalPay.Company);

            var items = ExecuteProcess<GetAdditionalPay, IEnumerable<AdditionalPay>>(new GetAdditionalPay
            {
                Company = downloadAdditionalPay.Company,
                NumPeriod = downloadAdditionalPay.NumPeriod,
                TypeNomina = downloadAdditionalPay.TypeNomina,
                Tenant = downloadAdditionalPay.Tenant
            });

            if (downloadAdditionalPay.TypeFileDownload == TypeFileDownload.PDF)
            {
                return _additionalPayPdfService.Generate(items, company?.Name ?? "", $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}");
            }

            return GenerateAdditionalPayExcel(items);
        }

        private static byte[] GenerateAdditionalPayExcel(IEnumerable<AdditionalPay> items)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Pagos Adicionales");
            var index = 1;

            // Headers
            worksheet.Cell($"A{index}").Value = "Empleado";
            worksheet.Cell($"B{index}").Value = "Fecha";
            worksheet.Cell($"C{index}").Value = "Código de Incidencia";
            worksheet.Cell($"D{index}").Value = "Columna";
            worksheet.Cell($"E{index}").Value = "Valor Base";
            worksheet.Cell($"F{index}").Value = "Operador";
            worksheet.Cell($"G{index}").Value = "Valor de Operación";
            worksheet.Cell($"H{index}").Value = "Total";

            index++;

            // Configurar columnas
            worksheet.Column("B").Width = 10;
            worksheet.Column("B").Style.NumberFormat.Format = "dd/mm/yyyy";
            worksheet.Column("E").Width = 10;
            worksheet.Column("E").Style.NumberFormat.Format = "0.00";
            worksheet.Column("G").Width = 10;
            worksheet.Column("G").Style.NumberFormat.Format = "0.00";
            worksheet.Column("H").Width = 10;
            worksheet.Column("H").Style.NumberFormat.Format = "0.00";

            foreach (var employee in items)
            {
                worksheet.Cell($"A{index}").Value = employee.EmployeeName;
                worksheet.Cell($"B{index}").Value = employee.Date.ToString("dd/MM/yyyy");
                worksheet.Cell($"C{index}").Value = employee.IncidentCode;
                worksheet.Cell($"D{index}").Value = employee.Column;
                worksheet.Cell($"E{index}").Value = employee.BaseValue.ToString("C");
                worksheet.Cell($"F{index}").Value = employee.OperatorText;
                worksheet.Cell($"G{index}").Value = employee.OperationValue.ToString("C");
                worksheet.Cell($"H{index}").Value = employee.Total.ToString("C");
                index++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExecuteProcess(DownloadAttendanceEmployee downloadAttendance)
        {
            var keyQuery = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Include(k => k.Tabulator)
                .Where(k => k.Company == downloadAttendance.Company && k.TypeNom == downloadAttendance.TypeNomina);

            var company = _companiesRepository.GetById(downloadAttendance.Company);

            if (downloadAttendance.Tenant != "-999")
            {
                keyQuery = _globalPropertyService.TypeTenant == TypeTenant.Department
                    ? keyQuery.Where(k => k.Center.Trim() == downloadAttendance.Tenant)
                    : keyQuery.Where(k => k.Supervisor == Convert.ToDecimal(downloadAttendance.Tenant));
            }

            var keysData = keyQuery
                .Select(k => new { k.Codigo, Activity = k.Tabulator.Activity ?? "" })
                .ToList();

            var employeeCodes = keysData.Select(k => k.Codigo).ToHashSet();
            var activityLookup = keysData.ToDictionary(k => k.Codigo, k => k.Activity);

            var year = _globalPropertyService.YearOfOperation;
            var period = _periodRepository.GetByFilter(
                p => p.TypePayroll == downloadAttendance.TypeNomina &&
                     p.Company == downloadAttendance.Company &&
                     p.NumPeriod == downloadAttendance.NumPeriod &&
                     p.Year == year
            ).AsQueryable().AsNoTracking().FirstOrDefault();

            var payroll = _payrollRepository.GetByFilter(
                p => p.Company == company!.Id && p.TypeNom == downloadAttendance.TypeNomina
            ).First();

            if (period == null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            // Obtener nombre de tenant
            var tenantName = "Todos";
            if (downloadAttendance.Tenant != "-999")
            {
                tenantName = _globalPropertyService.TypeTenant == TypeTenant.Department
                    ? _centerRepository.GetByFilter(c => c.Id.Trim() == downloadAttendance.Tenant && c.Company == company!.Id)
                        .FirstOrDefault()?.DepartmentName ?? ""
                    : _supervisorRepository.GetByFilter(s => s.Id == int.Parse(downloadAttendance.Tenant!))
                        .FirstOrDefault()?.Name ?? "";
            }

            // Obtener incidentes optimizado
            var assistanceIncidents = _assistanceIncident.GetContextEntity()
                .AsNoTracking()
                .Where(ai => employeeCodes.Contains(ai.EmployeeCode) &&
                             ai.CompanyId == downloadAttendance.Company &&
                             ai.Date >= period.StartDate &&
                             ai.Date <= period.ClosingDate)
                .Select(ai => new
                {
                    ai.Id,
                    ai.EmployeeCode,
                    ai.CompanyId,
                    ai.Date,
                    ai.IncidentCode,
                    ai.Approved,
                    ai.TimeOffRequest,
                    ai.UpdatedAt,
                    IsAdditional = ai.ItemIncidentCode != null && ai.ItemIncidentCode.IsAdditional,
                    Label = ai.ItemIncidentCode != null ? ai.ItemIncidentCode.Label : ""
                })
                .ToList();

            var incidentsLookup = assistanceIncidents
                .GroupBy(ai => (ai.EmployeeCode, ai.Date))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Obtener empleados
            var employees = _employeeService.GetByFilter(
                e => employeeCodes.Contains(e.Codigo) && e.Company == downloadAttendance.Company && e.Active == 'S'
            ).ToList();

            var codesToFilter = employees.Select(e => e.Codigo).ToHashSet();

            // Obtener checkins
            var attendances = _employeeCheckIns.GetContextEntity()
                .AsNoTracking()
                .Where(a => a.CheckIn != TimeOnly.MinValue &&
                            a.Date >= period.StartDate &&
                            a.Date <= period.ClosingDate &&
                            codesToFilter.Contains(a.EmployeeCode) &&
                            a.CompanyId == downloadAttendance.Company)
                .Select(a => new
                {
                    a.EmployeeCode,
                    a.CompanyId,
                    a.Date,
                    a.CheckIn,
                    a.EoS,
                    a.TypeNom
                })
                .ToList();

            var checkinsLookup = attendances
                .GroupBy(a => (a.EmployeeCode, a.Date))
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CheckIn).ToList());

            var listDates = DateService.GetListDate(period.StartDate, period.ClosingDate);

            var employeeAttendancesResult = employees.Select(emp =>
            {
                var empAttendances = listDates.Select(date =>
                {
                    var key = (emp.Codigo, date);
                    var checks = checkinsLookup.TryGetValue(key, out var c) ? c : null;
                    var incidents = incidentsLookup.TryGetValue(key, out var i) ? i : new List<dynamic>();

                    var defaultIncident = incidents.FirstOrDefault(ai => !ai.IsAdditional && ai.Approved);

                    return new AttendanceOutput
                    {
                        Date = date,
                        IncidentCode = defaultIncident?.IncidentCode ?? "--:--",
                        IncidentCodeLabel = defaultIncident?.Label ?? "",
                        TypeNom = checks?.FirstOrDefault()?.TypeNom ?? 0,
                        CheckEntry = checks?.FirstOrDefault(c => c.EoS == EntryOrExit.Entry)?.CheckIn.ToString("HH:mm"),
                        CheckOut = checks?.LastOrDefault(c => c.EoS == EntryOrExit.Exit)?.CheckIn.ToString("HH:mm"),
                        AssistanceIncidents = incidents.Select(ai => new AssistanceIncidentOutput
                        {
                            Id = ai.Id,
                            Date = ai.Date,
                            IncidentCode = ai.IncidentCode,
                            Approved = ai.Approved,
                            IsAdditional = ai.IsAdditional,
                            Label = ai.Label,
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
                    Activity = activityLookup.TryGetValue(emp.Codigo, out var activity) ? activity : "",
                    Attendances = empAttendances,
                };
            }).ToList();

            if (downloadAttendance.TypeFileDownload == TypeFileDownload.PDF)
            {
                var findObject = _sysConfigService.ExecuteProcess<GetConfigReport, SysConfigReports>(new GetConfigReport { });

                if (findObject.ConfigAttendanceReport.TypeAttendanceReportPdf == TypeAttendanceReportPdf.Standard)
                {
                    return _pdfService.GenerateAttendance(employeeAttendancesResult, company?.Name ?? "", tenantName,
                        $"{period.StartDate} - {period.ClosingDate}", listDates,
                        $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}",
                        $"{payroll.TypeNom} - {payroll.Label}");
                }

                return _attendancePdfService.Generate(employeeAttendancesResult, company?.Name ?? "", tenantName,
                    $"{period.StartDate} - {period.ClosingDate}", listDates,
                    $"RFC: {company!.RFC} | R. Patronal: {company.EmployerRegistration}",
                    $"{payroll.TypeNom} - {payroll.Label}");
            }

            return GenerateAttendanceExcel(employeeAttendancesResult, period, listDates);
        }

        private static byte[] GenerateAttendanceExcel(
            List<EmployeeAttendancesOutput> employees,
            Models.Prenomina.Period period,
            List<DateOnly> listDates)
        {
            var listAdminsDates = DateService.GetListDate(period.StartAdminDate, period.ClosingAdminDate);
            var incidentsApply = SysConfig.IncidentApplyToAttendance;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("T. Asistencia");
            var index = 1;

            // Headers
            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "conc";
            worksheet.Cell($"C{index}").Value = "importe";
            worksheet.Cell($"D{index}").Value = "fecha";
            worksheet.Cell($"E{index}").Value = "tipo";
            worksheet.Cell($"F{index}").Value = "dias";

            index++;

            // Configurar columnas
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

            foreach (var employee in employees)
            {
                var onlyIncidentApply = employee.Attendances?
                    .Where(a => incidentsApply.Contains(a.IncidentCode))
                    .ToList() ?? new List<AttendanceOutput>();

                foreach (var attendance in onlyIncidentApply)
                {
                    var indexDate = listDates.IndexOf(attendance.Date);

                    if (indexDate >= 0 && indexDate < listAdminsDates.Count)
                    {
                        var findAdminDate = listAdminsDates[indexDate];

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

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
