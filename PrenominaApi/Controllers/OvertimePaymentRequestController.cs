using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input.OvertimePayment;
using PrenominaApi.Models.Dto.Output.OvertimePayment;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class OvertimePaymentRequestController : ControllerBase
    {
        private readonly OvertimePaymentRequestService _service;

        public OvertimePaymentRequestController(OvertimePaymentRequestService service)
        {
            _service = service;
        }

        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return (int)decimal.Parse(company);
        }

        [HttpGet]
        public async Task<ActionResult<List<OvertimePaymentRequestOutput>>> Get()
        {
            var result = await _service.GetList(GetCompanyId());
            return Ok(result);
        }

        [HttpGet("{id}/detail")]
        public async Task<ActionResult<OvertimePaymentRequestDetailOutput>> Detail(string id)
        {
            var result = await _service.GetDetail(Guid.Parse(id));
            return Ok(result);
        }

        [HttpPost("{id}/reresolve")]
        public async Task<ActionResult<int>> ReResolve(string id)
        {
            var changed = await _service.ReResolve(Guid.Parse(id));
            return Ok(new { changed });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id)
        {
            var result = await _service.GeneratePdf(Guid.Parse(id));
            return File(result.Bytes, "application/pdf", result.FileName);
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult<bool>> Approve(string id, [FromBody] ChangeOvertimePaymentStatus input)
        {
            var result = await _service.ChangeStatus(Guid.Parse(id), approve: true, comment: input?.Comment);
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult<bool>> Reject(string id, [FromBody] ChangeOvertimePaymentStatus input)
        {
            var result = await _service.ChangeStatus(Guid.Parse(id), approve: false, comment: input?.Comment);
            return Ok(result);
        }
    }
}
