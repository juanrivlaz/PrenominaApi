using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services;
using System.Linq.Expressions;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IBaseService<Employee> _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public EmployeesController(IBaseService<Employee> service, GlobalPropertyService globalPropertyService)
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
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
    }
}
