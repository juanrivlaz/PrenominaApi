using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly IBaseServicePrenomina<SystemConfig> _service;

        public SystemConfigController(IBaseServicePrenomina<SystemConfig> service)
        {
            _service = service;
        }

        [HttpPut("clock-interval")]
        public ActionResult<bool> UpdateClockInterval([FromBody] ClockInterval clockInterval)
        {
            var result = _service.ExecuteProcess<ClockInterval, bool>(clockInterval);

            return Ok(result);
        }

        [HttpPut("logo")]
        public ActionResult<bool> UpdateLogo([FromBody] UpdateLogo updateLogo)
        {
            var result = _service.ExecuteProcess<UpdateLogo, bool>(updateLogo);

            return Ok(result);
        }

        [HttpPut("type-tenant")]
        public ActionResult<bool> UpdateTypeTenant([FromBody] UpdateTypeTenant updateTypeTenant)
        {
            var result = _service.ExecuteProcess<UpdateTypeTenant, bool>(updateTypeTenant);

            return Ok(result);
        }
    }
}
