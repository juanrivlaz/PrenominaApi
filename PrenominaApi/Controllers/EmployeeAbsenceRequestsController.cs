using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input.EmployeeAbsenceRequest;
using PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]"), Authorize]
    public class EmployeeAbsenceRequestsController : ControllerBase
    {
        private readonly IBaseServicePrenomina<EmployeeAbsenceRequests> _service;

        public EmployeeAbsenceRequestsController(IBaseServicePrenomina<EmployeeAbsenceRequests> service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<EmployeeAbsenceRequestOutput>> Get()
        {
            string? headerCompany = HttpContext.Items["companySelected"]?.ToString();
            var company = decimal.Parse(headerCompany ?? "0");
            var result = _service.ExecuteProcess<decimal, IEnumerable<EmployeeAbsenceRequestOutput>>(company);

            return Ok(result);
        }

        [HttpPut("{id}/status")]
        public ActionResult<bool> ChangeStatus(string id, [FromBody] ChangeStatus changeStatus)
        {
            changeStatus.Id = id;

            var result = _service.ExecuteProcess<ChangeStatus, bool>(changeStatus);
            return Ok(result);
        }

        [HttpGet("{id}/download")]
        public IActionResult Download(string id)
        {
            var result = _service.ExecuteProcess<DownloadRequest, byte[]>(new DownloadRequest { Id = id });

            return File(
                result,
                "application/pdf",
                "Employee_Absence_Request.pdf"
            );
        }
    }
}
