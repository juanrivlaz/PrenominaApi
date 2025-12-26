using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IgnoreIncidentToEmployeeController : ControllerBase
    {
        private readonly IBaseServicePrenomina<IgnoreIncidentToEmployee> _service;

        public IgnoreIncidentToEmployeeController(IBaseServicePrenomina<IgnoreIncidentToEmployee> service)
        {
            _service = service;
        }

        [HttpPost]
        public ActionResult<bool> AddIgnoreIncidentToEmployee([FromBody] AddIgnoreIncidentToEmployee addIgnoreIncidentToEmployee)
        {
            var result = _service.ExecuteProcess<AddIgnoreIncidentToEmployee, bool>(addIgnoreIncidentToEmployee);

            return Ok(result);
        }

        [HttpGet("{employeeCode}")]
        public ActionResult<IEnumerable<IgnoreIncidentToEmployee>> GetByEmployeeId(int employeeCode)
        {
            var result = _service.GetByFilter((item) => item.EmployeeCode == employeeCode);

            return Ok(result);
        }
    }
}
