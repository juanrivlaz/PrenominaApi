using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    [ApiController]
    public class OvertimeAccumulationController : ControllerBase
    {
        private readonly OvertimeAccumulationService _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public OvertimeAccumulationController(
            OvertimeAccumulationService service,
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
        /// Obtiene el resumen de horas extras con opciones de acumulación
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<List<OvertimeSummaryOutput>>> GetSummary(
            [FromQuery] int typeNomina,
            [FromQuery] int numPeriod,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetOvertimeSummary(
                typeNomina,
                numPeriod,
                GetCompanyId(),
                GetTenant(),
                search);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el balance de acumulación de un empleado
        /// </summary>
        [HttpGet("balance/{employeeCode}")]
        public async Task<ActionResult<OvertimeAccumulationOutput>> GetBalance(int employeeCode)
        {
            var result = await _service.GetEmployeeAccumulation(
                employeeCode,
                GetCompanyId());

            if (result == null)
            {
                return Ok(new OvertimeAccumulationOutput
                {
                    EmployeeCode = employeeCode,
                    AvailableMinutes = 0,
                    AvailableFormatted = "0 hrs 00 min"
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Acumula horas extras
        /// </summary>
        [HttpPost("accumulate")]
        public async Task<ActionResult<OvertimeOperationResult>> Accumulate([FromBody] AccumulateOvertimeInput input)
        {
            var result = await _service.AccumulateOvertime(
                input,
                GetCompanyId(),
                _globalPropertyService.UserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Registra pago directo de horas extras
        /// </summary>
        [HttpPost("pay-direct")]
        public async Task<ActionResult<OvertimeOperationResult>> PayDirect([FromBody] PayOvertimeDirectInput input)
        {
            var result = await _service.PayOvertimeDirect(
                input,
                GetCompanyId(),
                _globalPropertyService.UserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Usa horas acumuladas para día de descanso
        /// </summary>
        [HttpPost("use-for-rest-day")]
        public async Task<ActionResult<OvertimeOperationResult>> UseForRestDay([FromBody] UseOvertimeForRestDayInput input)
        {
            var result = await _service.UseForRestDay(
                input,
                GetCompanyId(),
                _globalPropertyService.UserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Realiza un ajuste manual
        /// </summary>
        [HttpPost("adjust")]
        public async Task<ActionResult<OvertimeOperationResult>> ManualAdjustment([FromBody] ManualOvertimeAdjustmentInput input)
        {
            var result = await _service.ManualAdjustment(
                input,
                GetCompanyId(),
                _globalPropertyService.UserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Cancela un movimiento previo
        /// </summary>
        [HttpPost("cancel")]
        public async Task<ActionResult<OvertimeOperationResult>> CancelMovement([FromBody] CancelOvertimeMovementInput input)
        {
            var result = await _service.CancelMovement(
                input,
                GetCompanyId(),
                _globalPropertyService.UserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el historial de movimientos
        /// </summary>
        [HttpGet("movements")]
        public async Task<ActionResult<OvertimeMovementsPagedOutput>> GetMovements([FromQuery] GetOvertimeMovementsInput input)
        {
            var result = await _service.GetMovementHistory(
                input,
                GetCompanyId());

            return Ok(result);
        }

        /// <summary>
        /// Procesa horas extras en lote
        /// </summary>
        [HttpPost("process-batch")]
        public async Task<ActionResult<List<OvertimeOperationResult>>> ProcessBatch([FromBody] ProcessOvertimesBatchInput input)
        {
            var results = await _service.ProcessOvertimesBatch(
                input,
                GetCompanyId(),
                GetTenant(),
                _globalPropertyService.UserId);

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            return Ok(new
            {
                TotalProcessed = results.Count,
                SuccessCount = successCount,
                FailCount = failCount,
                Details = results
            });
        }
    }
}
