using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    [ApiController]
    public class OvertimePaymentFileController : ControllerBase
    {
        private readonly OvertimePaymentFileService _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public OvertimePaymentFileController(
            OvertimePaymentFileService service,
            GlobalPropertyService globalPropertyService)
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return int.Parse(company);
        }

        private string GetTenant()
        {
            return HttpContext.Items["tenantSelected"]?.ToString() ?? "";
        }

        /// <summary>
        /// Previsualiza los renglones de pago de tiempo extra autorizado (concepto/importe/fecha/horas).
        /// </summary>
        [HttpGet("preview")]
        public async Task<IActionResult> Preview(
            [FromQuery] int typeNomina,
            [FromQuery] int numPeriod)
        {
            var lines = await _service.BuildLines(typeNomina, numPeriod, GetCompanyId(), GetTenant());
            return Ok(lines);
        }

        /// <summary>
        /// Indica si el archivo del periodo ya fue generado (indicador anti doble-pago).
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> Status(
            [FromQuery] int typeNomina,
            [FromQuery] int numPeriod)
        {
            var status = await _service.GetGenerationStatus(typeNomina, numPeriod, GetCompanyId());
            return Ok(status);
        }

        /// <summary>
        /// Descarga el archivo XLSX de importación de tiempo extra autorizado
        /// (CODIGO | CONCEPTO | IMPORTE | FECHA | HORAS) y marca el periodo como generado.
        /// </summary>
        [HttpGet("download")]
        public async Task<IActionResult> Download(
            [FromQuery] int typeNomina,
            [FromQuery] int numPeriod)
        {
            var companyId = GetCompanyId();
            var lines = await _service.BuildLines(typeNomina, numPeriod, companyId, GetTenant());
            var (fileName, content) = _service.GenerateFile(lines, numPeriod);

            await _service.RecordGeneration(typeNomina, numPeriod, companyId, _globalPropertyService.UserId, lines);

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
