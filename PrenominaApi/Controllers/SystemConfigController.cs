using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Reports;
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

        [HttpPut("type-day-off-report")]
        public ActionResult<bool> UpdateTypeDayOffReport([FromBody] EditTypeDayOffReport editTypeDayOffReport)
        {
            var result = _service.ExecuteProcess<EditTypeDayOffReport, bool>(editTypeDayOffReport);

            return Ok(result);
        }

        [HttpPut("min-to-overtime-report")]
        public ActionResult<bool> UpdateMinsToOvertimeReport([FromBody] EditMinsToOvertimeReport editMinsToOvertimeReport)
        {
            var result = _service.ExecuteProcess<EditMinsToOvertimeReport, bool>(editMinsToOvertimeReport);

            return Ok(result);
        }

        [HttpGet("config-reports")]
        public ActionResult<SysConfigReports> GetConfigReports()
        {
            var result = _service.ExecuteProcess<GetConfigReport, SysConfigReports>(new GetConfigReport() { });
            return Ok(result);
        }
    }
}
