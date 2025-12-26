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
    public class RolesController : ControllerBase
    {
        private readonly IBaseServicePrenomina<Role> _service;

        public RolesController(IBaseServicePrenomina<Role> service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Role>> Get()
        {
            var result = _service.GetAll();

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<Role> Store([FromBody] CreateRole createRole)
        {
            var result = _service.ExecuteProcess<CreateRole, Role>(createRole);

            return Ok(result);
        }
    }
}
