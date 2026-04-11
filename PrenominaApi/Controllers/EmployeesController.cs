using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Filters;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services;
using PrenominaApi.Services.Prenomina;
using System.Linq.Expressions;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    public class EmployeesController : ControllerBase
    {
        private readonly IBaseService<Employee> _service;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly PrenominaDbContext _context;
        private readonly WorkScheduleService _workScheduleService;

        public EmployeesController(
            IBaseService<Employee> service,
            GlobalPropertyService globalPropertyService,
            PrenominaDbContext context,
            WorkScheduleService workScheduleService)
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
            _context = context;
            _workScheduleService = workScheduleService;
        }

        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return int.Parse(company);
        }

        // GET: api/Employees
        [HttpGet]
        public ActionResult<IEnumerable<Employee>> GetEmployees([FromQuery] Paginator paginator)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            Func<Employee, bool> filter = employee => employee.Company == companyId;

            if (!string.IsNullOrWhiteSpace(paginator.Search))
            {
                var searchTerm = paginator.Search.ToLower();
                filter = employee =>
                    employee.Company == companyId &&
                    ($"{employee.Name} {employee.LastName} {employee.MLastName}".ToLower().Contains(searchTerm));
            }

            var result = _service.GetWithPagination(paginator.Page, paginator.PageSize, filter);

            return Ok(result);
        }

        [HttpGet("overtime-configs")]
        [Authorize]
        public async Task<ActionResult> GetOvertimeConfigs()
        {
            var companyId = GetCompanyId();

            var configs = await _context.employeeOvertimeConfigs
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.ExcludeOvertime)
                .Select(c => c.EmployeeCode)
                .ToListAsync();

            return Ok(configs);
        }

        [HttpPut("{employeeCode}/exclude-overtime")]
        [Authorize]
        public async Task<ActionResult<bool>> UpdateExcludeOvertime(int employeeCode, [FromBody] ExcludeOvertimeInput input)
        {
            var companyId = GetCompanyId();

            var config = await _context.employeeOvertimeConfigs
                .FirstOrDefaultAsync(c => c.EmployeeCode == employeeCode && c.CompanyId == companyId);

            if (config == null)
            {
                config = new Models.Prenomina.EmployeeOvertimeConfig
                {
                    EmployeeCode = employeeCode,
                    CompanyId = companyId,
                    ExcludeOvertime = input.ExcludeOvertime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.employeeOvertimeConfigs.Add(config);
            }
            else
            {
                config.ExcludeOvertime = input.ExcludeOvertime;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(true);
        }

        [HttpGet("by-payroll")]
        public ActionResult<PagedResult<EmployeeOutput>> GetByPayroll([FromQuery] FilterEmployeesByPayroll filter)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            filter.CompanyId = companyId;
            filter.Tenant = tenant;

            var result = _service.ExecuteProcess<FilterEmployeesByPayroll, PagedResult<EmployeeOutput>>(filter);

            return Ok(result);
        }

        [HttpGet("schedule-assignments")]
        [Authorize]
        public ActionResult GetScheduleAssignments()
        {
            return Ok(_workScheduleService.GetActiveEmployeeAssignments(GetCompanyId()));
        }

        [HttpPut("{employeeCode}/work-schedule")]
        [Authorize]
        public ActionResult<bool> UpdateEmployeeSchedule(int employeeCode, [FromBody] AssignEmployeeWorkScheduleInput input)
        {
            var effectiveFrom = input.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return Ok(_workScheduleService.AssignEmployeeSchedule(employeeCode, GetCompanyId(), input.WorkScheduleId, effectiveFrom));
        }

        [HttpGet("{employeeCode}/work-schedule/history")]
        [Authorize]
        public ActionResult GetEmployeeScheduleHistory(int employeeCode)
        {
            return Ok(_workScheduleService.GetEmployeeAssignmentHistory(employeeCode, GetCompanyId()));
        }
    }
}
