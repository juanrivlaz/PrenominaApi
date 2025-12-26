using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IBaseServicePrenomina<User> _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public UserController(
            IBaseServicePrenomina<User> service,
            GlobalPropertyService globalPropertyService
        ) {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<User>> Get()
        {
            var result = _service.ExecuteProcess<GetAllUser, IEnumerable<User>>(new GetAllUser() { });

            return Ok(result);
        }

        [HttpGet("init")]
        public ActionResult<Models.Dto.Output.InitCreateUser> GetInit()
        {
            var result = _service.ExecuteProcess<Models.Dto.Input.InitCreateUser, Models.Dto.Output.InitCreateUser>(new Models.Dto.Input.InitCreateUser() { });

            return Ok(result);
        }

        [HttpGet("me")]
        public ActionResult<ResultLogin> GetMe()
        {
            var userId = HttpContext.User.FindFirst("UserId")?.Value;
            var roleCode = HttpContext.User.FindFirst("RoleCode")?.Value;
            UserDetails? userDetails = HttpContext.Items["UserDetails"] as UserDetails;
            if (roleCode == RoleCode.Sudo)
            {
                userDetails = _service.ExecuteProcess<string, UserDetails>(userId!);
            }

            return Ok(new ResultLogin()
            {
                Token = "",
                TypeTenant = _globalPropertyService.TypeTenant,
                UserDetails = userDetails!,
                Username = ""
            });
        }

        [HttpPost]
        public ActionResult<User> Store([FromBody] CreateUser user) {
            var result = _service.ExecuteProcess<CreateUser, User>(user);

            return Ok(result);
        }

        [HttpPut("{userId}")]
        public ActionResult<User> Edit(string userId, [FromBody] EditUser editUser)
        {
            editUser.UserId = userId;
            var result = _service.ExecuteProcess<EditUser, User>(editUser);

            return Ok(result);
        }

        [HttpDelete("{userId}")]
        public ActionResult<bool> Delete(string userId)
        {
            var result = _service.ExecuteProcess<DeleteUser, bool>(new DeleteUser()
            {
              UserId = userId
            });

            return Ok(result);
        }
    }
}
