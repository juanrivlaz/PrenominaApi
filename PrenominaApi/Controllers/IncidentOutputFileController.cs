using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class IncidentOutputFileController : ControllerBase
    {
        private readonly IBaseServicePrenomina<IncidentOutputFile> _service;

        public IncidentOutputFileController(IBaseServicePrenomina<IncidentOutputFile> service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<IncidentOutputFile>> Get()
        {
            var result = _service.GetAll();

            return Ok(result);
        }

        [HttpGet("generate-incident-file")]
        public IActionResult GenerateIncidentFile()
        {
            var result = _service.ExecuteProcess<string, byte[]>("code");
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "incident_code.xlsx";

            return this.File(
                fileContents: result,
                contentType,
                fileDownloadName: fileName
            );
        }

        [HttpPost]
        public ActionResult<IncidentOutputFile> Store([FromBody] CreateIncidentOutputFile createIncidentOutputFile)
        {
            var result = _service.ExecuteProcess<CreateIncidentOutputFile, IncidentOutputFile>(createIncidentOutputFile);

            return Ok(result);
        }
    }
}
