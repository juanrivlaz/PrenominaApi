using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Reports;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Reports;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using System.Data;
using System.Text.Json;

namespace PrenominaApi.Services.Prenomina
{
    public class ReportService : ServicePrenomina<SysConfigReports>
    {
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepositoryPrenomina<SystemConfig> _sysConfigRepository;
        private readonly IBaseRepositoryPrenomina<AssistanceIncident> _assistanceIncidentRepository;
        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodRepository;
        private readonly GlobalPropertyService _globalPropertyService;

        public ReportService(
            IBaseRepositoryPrenomina<SysConfigReports> baseRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodRepository,
            IBaseRepositoryPrenomina<SystemConfig> sysConfigRepository,
            IBaseRepositoryPrenomina<AssistanceIncident> assistanceIncidentRepository,
            IBaseRepository<Key> keyRepository,
            GlobalPropertyService globalPropertyService
        ) : base(baseRepository)
        {
            _keyRepository = keyRepository;
            _periodRepository = periodRepository;
            _globalPropertyService = globalPropertyService;
            _sysConfigRepository = sysConfigRepository;
            _assistanceIncidentRepository = assistanceIncidentRepository;
        }

        public IEnumerable<ReportDelaysOutput> ExecuteProcess(GetReportDelays getReport)
        {
            DateOnly StartDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly ClosingDate = DateOnly.FromDateTime(DateTime.Now);

            var year = _globalPropertyService.YearOfOperation;
            var lowerSearch = getReport.Search?.ToLower();

            if (getReport.FilterDates != null)
            {
                StartDate = DateOnly.FromDateTime(getReport.FilterDates.Start);
                ClosingDate = DateOnly.FromDateTime(getReport.FilterDates.End);
            } else
            {
                var periodDates = _periodRepository.GetByFilter(
                    (period) => period.TypePayroll == getReport.TypeNomina &&
                    period.Company == getReport.Company &&
                    period.NumPeriod == getReport.NumPeriod &&
                    period.Year == year).FirstOrDefault();

                if (periodDates is null)
                {
                    throw new BadHttpRequestException("El periodo seleccionado no es válido.");
                }

                StartDate = periodDates.StartDate;
                ClosingDate = periodDates.ClosingDate;
            }

            var employees = _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == getReport.Company &&
                    k.TypeNom == getReport.TypeNomina &&
                    (
                        getReport.Tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == getReport.Tenant :
                            k.Supervisor == Convert.ToDecimal(getReport.Tenant)
                        )
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (
                            k.Employee.Name + " " +
                            k.Employee.LastName + " " +
                            k.Employee.MLastName
                        ).ToLower().Contains(lowerSearch)
                    )
                ).Select(k => new
                {
                    k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                }).ToList();

            if (!employees.Any())
            {
                return Enumerable.Empty<ReportDelaysOutput>();
            }

            var employeeDict = employees.ToDictionary(e => e.Codigo);
            var employeeCodesJson = JsonSerializer.Serialize(employeeDict.Keys);
            var resultQuery = _repository.GetDbContext().Database.SqlQueryRaw<ScheduleEmployeeOutput>(
                """
                SELECT
                    eci.employee_code AS Code,
                    MIN(eci.check_in) AS CheckIn,
                    eci.[date] AS [Date],
                    ws.start_time AS StartTime,
                    DATEDIFF(MINUTE, ws.start_time, eci.check_in) AS MinsLate,
                    checkout.CheckOut
                FROM employee_check_ins AS eci
                CROSS APPLY (
                    SELECT TOP 1 *
                    FROM work_schedule AS ws
                    WHERE ws.company = @company
                    ORDER BY ABS(DATEDIFF(MINUTE, ws.start_time, eci.check_in))
                ) AS ws
                OUTER APPLY (
                    SELECT MAX(ec2.check_in) AS CheckOut
                    FROM employee_check_ins AS ec2
                    WHERE ec2.employee_code = eci.employee_code
                    AND ec2.[date] = eci.[date]
                    AND ec2.EoS = 1
                ) AS checkout
                WHERE
                    eci.EoS = 0
                    AND DATEDIFF(MINUTE, ws.start_time, eci.check_in) > 20
                    AND eci.[date] BETWEEN @startDate AND @closingDate
                    AND eci.employee_code IN (
                        SELECT value FROM OPENJSON(@codes)
                    )
                GROUP BY
                    eci.employee_code,
                    eci.[date],
                    ws.start_time,
                    eci.check_in,
                    checkout.CheckOut
                ORDER BY
                    DATEDIFF(MINUTE, ws.start_time, eci.check_in),
                    eci.employee_code;
                """,
                new SqlParameter("@company", getReport.Company),
                new SqlParameter("@codes", employeeCodesJson),
                new SqlParameter("@startDate", StartDate),
                new SqlParameter("@closingDate", ClosingDate)
            ).ToList();

            var result = new List<ReportDelaysOutput>(resultQuery.Count);

            foreach (var item in resultQuery)
            {
                if (!employeeDict.TryGetValue(item.Code, out var employee))
                {
                    continue;
                }

                result.Add(new ReportDelaysOutput
                {
                    Code = item.Code,
                    Date = item.Date,
                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,
                    Department = employee.Department ?? string.Empty,
                    FullName = employee.FullName,
                    JobPosition = employee.JobPosition ?? string.Empty,
                    TimeDelayed = item.MinsLate
                });
            }


            return result;
        }

        public IEnumerable<ReportOvertimesOutput> ExecuteProcess(GetReportOvertimes getReport)
        {
            var configReport = _sysConfigRepository.GetById(SysConfig.ConfigReports);
            var mins = 30;
            if (configReport != null)
            {
                var parser = JsonSerializer.Deserialize<SysConfigReports>(configReport.Data);
                if (parser != null)
                {
                    mins = parser.ConfigOvertimeReport.Mins;
                }
            }

            DateOnly StartDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly ClosingDate = DateOnly.FromDateTime(DateTime.Now);

            var year = _globalPropertyService.YearOfOperation;
            var lowerSearch = getReport.Search?.ToLower();

            if (getReport.FilterDates != null)
            {
                StartDate = DateOnly.FromDateTime(getReport.FilterDates.Start);
                ClosingDate = DateOnly.FromDateTime(getReport.FilterDates.End);
            }
            else
            {
                var periodDates = _periodRepository.GetByFilter(
                    (period) => period.TypePayroll == getReport.TypeNomina &&
                    period.Company == getReport.Company &&
                    period.NumPeriod == getReport.NumPeriod &&
                    period.Year == year).FirstOrDefault();

                if (periodDates is null)
                {
                    throw new BadHttpRequestException("El periodo seleccionado no es válido.");
                }

                StartDate = periodDates.StartDate;
                ClosingDate = periodDates.ClosingDate;
            }

            var employees = _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == getReport.Company &&
                    k.TypeNom == getReport.TypeNomina &&
                    (
                        getReport.Tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == getReport.Tenant :
                            k.Supervisor == Convert.ToDecimal(getReport.Tenant)
                        )
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (
                            k.Employee.Name + " " +
                            k.Employee.LastName + " " +
                            k.Employee.MLastName
                        ).ToLower().Contains(lowerSearch)
                    )
                ).Select(k => new
                {
                    k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                }).ToList();

            if (!employees.Any())
            {
                return Enumerable.Empty<ReportOvertimesOutput>();
            }

            var employeeDict = employees.ToDictionary(e => e.Codigo);
            var employeeCodesJson = JsonSerializer.Serialize(employeeDict.Keys);
            var resultQuery = _repository.GetDbContext().Database.SqlQueryRaw<ReportOvertimesOutput>(
                """
                SELECT
                    eci.employee_code AS Code,
                    MIN(eci.check_in) AS CheckIn,
                    checkout.CheckOut,
                    eci.[date] AS [Date],
                    DATEDIFF(MINUTE, eci.check_in, checkout.CheckOut) AS Overtime,
                    '' AS Department,
                    '' AS FullName,
                    '' AS JobPosition
                FROM employee_check_ins AS eci
                CROSS APPLY (
                    SELECT MAX(ec2.check_in) AS CheckOut
                    FROM employee_check_ins AS ec2
                    WHERE ec2.employee_code = eci.employee_code
                    AND ec2.[date] = eci.[date]
                    AND ec2.EoS = 1
                ) AS checkout
                WHERE
                    eci.EoS = 0
                    AND checkout.CheckOut IS NOT NULL
                    AND DATEDIFF(MINUTE, eci.check_in, checkout.CheckOut) >= (60 * 8) + @mins
                    AND eci.[date] BETWEEN @startDate AND @closingDate
                    AND eci.employee_code IN (
                        SELECT value FROM OPENJSON(@codes)
                    )
                GROUP BY
                    eci.employee_code,
                    eci.[date],
                    eci.check_in,
                    checkout.CheckOut
                ORDER BY
                    eci.employee_code;
                """,
                new SqlParameter("@company", getReport.Company),
                new SqlParameter("@codes", employeeCodesJson),
                new SqlParameter("@startDate", StartDate),
                new SqlParameter("@closingDate", ClosingDate),
                new SqlParameter("@mins", mins)
            ).ToList();

            var result = new List<ReportOvertimesOutput>(resultQuery.Count);

            foreach (var item in resultQuery)
            {
                if (!employeeDict.TryGetValue(item.Code, out var employee))
                {
                    continue;
                }

                result.Add(new ReportOvertimesOutput
                {
                    Code = item.Code,
                    Date = item.Date,
                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,
                    Department = employee.Department ?? string.Empty,
                    FullName = employee.FullName,
                    JobPosition = employee.JobPosition ?? string.Empty,
                    Overtime = item.Overtime
                });
            }


            return result;
        }

        public IEnumerable<ReportHoursWorkedOutput> ExecuteProcess(GetReportHoursWorked getReport)
        {
            DateOnly StartDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly ClosingDate = DateOnly.FromDateTime(DateTime.Now);

            var year = _globalPropertyService.YearOfOperation;
            var lowerSearch = getReport.Search?.ToLower();

            if (getReport.FilterDates != null)
            {
                StartDate = DateOnly.FromDateTime(getReport.FilterDates.Start);
                ClosingDate = DateOnly.FromDateTime(getReport.FilterDates.End);
            }
            else
            {
                var periodDates = _periodRepository.GetByFilter(
                    (period) => period.TypePayroll == getReport.TypeNomina &&
                    period.Company == getReport.Company &&
                    period.NumPeriod == getReport.NumPeriod &&
                    period.Year == year).FirstOrDefault();

                if (periodDates is null)
                {
                    throw new BadHttpRequestException("El periodo seleccionado no es válido.");
                }

                StartDate = periodDates.StartDate;
                ClosingDate = periodDates.ClosingDate;
            }

            var employees = _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == getReport.Company &&
                    k.TypeNom == getReport.TypeNomina &&
                    (
                        getReport.Tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == getReport.Tenant :
                            k.Supervisor == Convert.ToDecimal(getReport.Tenant)
                        )
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (
                            k.Employee.Name + " " +
                            k.Employee.LastName + " " +
                            k.Employee.MLastName
                        ).ToLower().Contains(lowerSearch)
                    )
                ).Select(k => new
                {
                    k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                }).ToList();

            if (!employees.Any())
            {
                return Enumerable.Empty<ReportHoursWorkedOutput>();
            }

            var employeeDict = employees.ToDictionary(e => e.Codigo);
            var employeeCodesJson = JsonSerializer.Serialize(employeeDict.Keys);
            var resultQuery = _repository.GetDbContext().Database.SqlQueryRaw<ReportHoursWorkedOutput>(
                """
                SELECT
                    eci.employee_code AS Code,
                    MIN(eci.check_in) AS CheckIn,
                    checkout.CheckOut,
                    eci.[date] AS [Date],
                    DATEDIFF(HOUR, eci.check_in, checkout.CheckOut) AS HoursWorked,
                    '' AS Department,
                    '' AS FullName,
                    '' AS JobPosition
                FROM employee_check_ins AS eci
                CROSS APPLY (
                    SELECT MAX(ec2.check_in) AS CheckOut
                    FROM employee_check_ins AS ec2
                    WHERE ec2.employee_code = eci.employee_code
                    AND ec2.[date] = eci.[date]
                    AND ec2.EoS = 1
                ) AS checkout
                WHERE
                    eci.EoS = 0
                    AND checkout.CheckOut IS NOT NULL
                    AND eci.[date] BETWEEN @startDate AND @closingDate
                    AND eci.employee_code IN (
                        SELECT value FROM OPENJSON(@codes)
                    )
                GROUP BY
                    eci.employee_code,
                    eci.[date],
                    eci.check_in,
                    checkout.CheckOut
                ORDER BY
                    eci.employee_code;
                """,
                new SqlParameter("@company", getReport.Company),
                new SqlParameter("@codes", employeeCodesJson),
                new SqlParameter("@startDate", StartDate),
                new SqlParameter("@closingDate", ClosingDate)
            ).ToList();

            var result = new List<ReportHoursWorkedOutput>(resultQuery.Count);

            foreach (var item in resultQuery)
            {
                if (!employeeDict.TryGetValue(item.Code, out var employee))
                {
                    continue;
                }

                result.Add(new ReportHoursWorkedOutput
                {
                    Code = item.Code,
                    Date = item.Date,
                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,
                    Department = employee.Department ?? string.Empty,
                    FullName = employee.FullName,
                    JobPosition = employee.JobPosition ?? string.Empty,
                    HoursWorked = item.HoursWorked
                });
            }

            return result;
        }

        public IEnumerable<ReportAttendanceOutput> ExecuteProcess(GetReportAttendance getReport)
        {
            DateOnly StartDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly ClosingDate = DateOnly.FromDateTime(DateTime.Now);

            var year = _globalPropertyService.YearOfOperation;
            var lowerSearch = getReport.Search?.ToLower();

            if (getReport.FilterDates != null)
            {
                StartDate = DateOnly.FromDateTime(getReport.FilterDates.Start);
                ClosingDate = DateOnly.FromDateTime(getReport.FilterDates.End);
            }
            else
            {
                var periodDates = _periodRepository.GetByFilter(
                    (period) => period.TypePayroll == getReport.TypeNomina &&
                    period.Company == getReport.Company &&
                    period.NumPeriod == getReport.NumPeriod &&
                    period.Year == year).FirstOrDefault();

                if (periodDates is null)
                {
                    throw new BadHttpRequestException("El periodo seleccionado no es válido.");
                }

                StartDate = periodDates.StartDate;
                ClosingDate = periodDates.ClosingDate;
            }

            var employees = _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == getReport.Company &&
                    k.TypeNom == getReport.TypeNomina &&
                    (
                        getReport.Tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == getReport.Tenant :
                            k.Supervisor == Convert.ToDecimal(getReport.Tenant)
                        )
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (
                            k.Employee.Name + " " +
                            k.Employee.LastName + " " +
                            k.Employee.MLastName
                        ).ToLower().Contains(lowerSearch)
                    )
                ).Select(k => new
                {
                    k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                }).ToList();

            if (!employees.Any())
            {
                return Enumerable.Empty<ReportAttendanceOutput>();
            }

            var employeeDict = employees.ToDictionary(e => e.Codigo);
            var employeeCodesJson = JsonSerializer.Serialize(employeeDict.Keys);
            var resultQuery = _repository.GetDbContext().Database.SqlQueryRaw<ReportHoursWorkedOutput>(
                """
                SELECT
                    eci.employee_code AS Code,
                    MIN(eci.check_in) AS CheckIn,
                    checkout.CheckOut,
                    eci.[date] AS [Date],
                    0 AS HoursWorked,
                    '' AS Department,
                    '' AS FullName,
                    '' AS JobPosition
                FROM employee_check_ins AS eci
                CROSS APPLY (
                    SELECT MAX(ec2.check_in) AS CheckOut
                    FROM employee_check_ins AS ec2
                    WHERE ec2.employee_code = eci.employee_code
                    AND ec2.[date] = eci.[date]
                    AND ec2.EoS = 1
                ) AS checkout
                WHERE
                    eci.EoS = 0
                    AND eci.[date] BETWEEN @startDate AND @closingDate
                    AND eci.employee_code IN (
                        SELECT value FROM OPENJSON(@codes)
                    )
                GROUP BY
                    eci.employee_code,
                    eci.[date],
                    eci.check_in,
                    checkout.CheckOut
                ORDER BY
                    eci.employee_code;
                """,
                new SqlParameter("@company", getReport.Company),
                new SqlParameter("@codes", employeeCodesJson),
                new SqlParameter("@startDate", StartDate),
                new SqlParameter("@closingDate", ClosingDate)
            ).ToList();

            var result = new List<ReportAttendanceOutput>(resultQuery.Count);

            foreach (var item in resultQuery)
            {
                if (!employeeDict.TryGetValue(item.Code, out var employee))
                {
                    continue;
                }

                result.Add(new ReportAttendanceOutput
                {
                    Code = item.Code,
                    Date = item.Date,
                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,
                    Department = employee.Department ?? string.Empty,
                    FullName = employee.FullName,
                    JobPosition = employee.JobPosition ?? string.Empty,
                });
            }

            return result;
        }
    
        public IEnumerable<ReportIncidencesOutput> ExecuteProcess(GetReportIncidences getReport)
        {
            DateOnly StartDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly ClosingDate = DateOnly.FromDateTime(DateTime.Now);

            var year = _globalPropertyService.YearOfOperation;
            var lowerSearch = getReport.Search?.ToLower();

            if (getReport.FilterDates != null)
            {
                StartDate = DateOnly.FromDateTime(getReport.FilterDates.Start);
                ClosingDate = DateOnly.FromDateTime(getReport.FilterDates.End);
            }
            else
            {
                var periodDates = _periodRepository.GetByFilter(
                    (period) => period.TypePayroll == getReport.TypeNomina &&
                    period.Company == getReport.Company &&
                    period.NumPeriod == getReport.NumPeriod &&
                    period.Year == year).FirstOrDefault();

                if (periodDates is null)
                {
                    throw new BadHttpRequestException("El periodo seleccionado no es válido.");
                }

                StartDate = periodDates.StartDate;
                ClosingDate = periodDates.ClosingDate;
            }

            var employees = _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == getReport.Company &&
                    k.TypeNom == getReport.TypeNomina &&
                    (
                        getReport.Tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == getReport.Tenant :
                            k.Supervisor == Convert.ToDecimal(getReport.Tenant)
                        )
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (
                            k.Employee.Name + " " +
                            k.Employee.LastName + " " +
                            k.Employee.MLastName
                        ).ToLower().Contains(lowerSearch)
                    )
                ).Select(k => new
                {
                    k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                }).ToList();

            if (!employees.Any())
            {
                return Enumerable.Empty<ReportIncidencesOutput>();
            }

            var incidents = _assistanceIncidentRepository.GetContextEntity().AsNoTracking()
                .Where(ai =>
                    ai.CompanyId == getReport.Company &&
                    ai.Date >= StartDate &&
                    ai.Date <= ClosingDate &&
                    employees.Select(e => e.Codigo).Contains(ai.EmployeeCode)
                ).Include(ai => ai.ItemIncidentCode).Include(ai => ai.User).ToList();

            if (!incidents.Any())
            {
                return Enumerable.Empty<ReportIncidencesOutput>();
            }

            return incidents.Select(ai =>
            {
               var employee = employees.FirstOrDefault(e => e.Codigo == ai.EmployeeCode);

                return new ReportIncidencesOutput
                {
                    Code = ai.EmployeeCode,
                    Date = ai.Date,
                    IncidenceCode = ai.ItemIncidentCode != null ? ai.ItemIncidentCode.Code : string.Empty,
                    IncidenceDescription = ai.ItemIncidentCode != null ? ai.ItemIncidentCode.Label : string.Empty,
                    Department = employee?.Department ?? string.Empty,
                    JobPosition = employee?.JobPosition ?? string.Empty,
                    CreatedAt = ai.CreatedAt,
                    FullName = employee?.FullName ?? string.Empty,
                    UserFullName = $"{ai?.User?.Name ?? string.Empty}"
                };
            });
        }
    }
}
