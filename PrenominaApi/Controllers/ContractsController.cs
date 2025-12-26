using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]"), Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IBaseService<Contract> _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public ContractsController(
            IBaseService<Contract> service,
            GlobalPropertyService globalPropertyService
        )
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ContractsOutput>> Get()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            var userId = HttpContext.User.FindFirst("UserId")?.Value ?? "";
            var filter = new ContractsInput()
            {
                CompanyId = Convert.ToDecimal(company),
                Tenant = "all",
                TypeNom = 1,
                TypeTenant = _globalPropertyService.TypeTenant,
                UserId = userId
            };

            var result = _service.ExecuteProcess<ContractsInput, IEnumerable<ContractsOutput>>(filter);

            return Ok(result);
        }

        [HttpPut("set-apply-new-contract")]
        public ActionResult<ContractsOutput> SetApplyNewContract([FromBody] SetApplyNewContract setApplyNewContract)
        {
            var result = _service.ExecuteProcess<SetApplyNewContract, ContractsOutput>(setApplyNewContract);

            return Ok(result);
        }

        [HttpGet("download")]
        public IActionResult Downlaod()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            var userId = HttpContext.User.FindFirst("UserId")?.Value ?? "";

            var result = _service.ExecuteProcess<DownloadContracts, byte[]>(new DownloadContracts() {
                Tenant = tenant,
                Company = Convert.ToDecimal(company),
                UserId = userId
            });

            return this.File(
                fileContents: result,
                contentType: "application/pdf",
                fileDownloadName: "Generar_Contratos.pdf"
            );
        }
    }
}
