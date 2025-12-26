using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IgnoreIncidentToActivityController : ControllerBase
    {
        private readonly IBaseServicePrenomina<IgnoreIncidentToActivity> _service;

        public IgnoreIncidentToActivityController(IBaseServicePrenomina<IgnoreIncidentToActivity> service)
        {
            _service = service;
        }

        [HttpPost]
        public ActionResult<bool> AddIgnoreIncidentToActivity([FromBody] AddIgnoreIncidentToActivity addIgnoreIncidentToActivity)
        {
            var result = _service.ExecuteProcess<AddIgnoreIncidentToActivity, bool>(addIgnoreIncidentToActivity);

            return Ok(result);
        }

        [HttpGet("{activityId}")]
        public ActionResult<IEnumerable<IgnoreIncidentToActivity>> GetByActivityId(int activityId)
        {
            var result = _service.GetByFilter((item) => item.ActivityId == activityId);

            return Ok(result);
        }

        [HttpGet("activities")]
        public ActionResult<IEnumerable<Tabulator>> GetActivities()
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            var result = _service.ExecuteProcess<int, IEnumerable<Tabulator>>(companyId);

            return Ok(result);
        }
    }
}
