using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;
using InputDto = PrenominaApi.Models.Dto.Input;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IBaseServicePrenomina<User> _service;

        public AuthController(IBaseServicePrenomina<User> service)
        {
            _service = service;
        }

        [HttpPost]
        public ActionResult<ResultLogin> Login([FromBody] InputDto.AuthLogin credentials)
        {
            var result = _service.ExecuteProcess<InputDto.AuthLogin, ResultLogin>(credentials);

            return Ok(result);
        }
    }
}
