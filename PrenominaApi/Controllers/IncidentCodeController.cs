using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output.IncidentCodes;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class IncidentCodeController : ControllerBase
    {
        private readonly IBaseServicePrenomina<IncidentCode> _service;
        private readonly IBaseServicePrenomina<User> _userService;
        private readonly IBaseServicePrenomina<Role> _roleService;
        private readonly GlobalPropertyService _globalPropertyService;

        public IncidentCodeController(
            IBaseServicePrenomina<IncidentCode> service,
            IBaseServicePrenomina<User> userService,
            IBaseServicePrenomina<Role> roleService,
            GlobalPropertyService globalPropertyService
        )
        {
            _service = service;
            _userService = userService;
            _roleService = roleService;
            _globalPropertyService = globalPropertyService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<IncidentCode>> Get()
        {
            var result = _service.ExecuteProcess<GetAllIncidentCode, IEnumerable<IncidentCode>>(new GetAllIncidentCode() {});

            return Ok(result);
        }

        [HttpGet("init")]
        public ActionResult<InitForCreateIncidentCode> GetIni()
        {
            var users = _userService.ExecuteProcess<GetUserByPermissionSection, IEnumerable<User>>(new GetUserByPermissionSection() { SectionCode = SectionCode.PendingsAttendanceIncident });
            var roles = _roleService.GetAll();

            return Ok(new InitForCreateIncidentCode()
            {
                Users = users,
                Roles = roles
            });
        }

        [HttpPost]
        public ActionResult<IncidentCode> Store([FromBody] CreateIncidentCode incidentCode)
        {
            var result = _service.ExecuteProcess<CreateIncidentCode, IncidentCode>(incidentCode);

            return Ok(result);
        }

        [HttpPut("{code}")]
        public ActionResult<IncidentCode> Edit(string code, [FromBody] EditIncidentCode editIncidentCode)
        {
            editIncidentCode.Id = code;
            var result = _service.ExecuteProcess<EditIncidentCode, IncidentCode>(editIncidentCode);

            return Ok(result);
        }
    }
}
