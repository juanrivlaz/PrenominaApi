using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClocksController : ControllerBase
    {
        private readonly IBaseServicePrenomina<Clock> _service;

        public ClocksController(IBaseServicePrenomina<Clock> service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Clock>> Get()
        {
            var result = _service.GetAll();

            return Ok(result);
        }

        [HttpGet("get-clock-user/{clockId}")]
        public async Task<ActionResult<IEnumerable<ClockUser>>> GetClockUser(string clockId)
        {
            var result = await _service.ExecuteProcess<GetClockUser, Task<IEnumerable<ClockUser>>>(new GetClockUser()
            {
                Id = Guid.Parse(clockId),
            });

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<Clock> Store([FromBody] CreateClock createClock)
        {
            var result = _service.ExecuteProcess<CreateClock, Clock>(createClock);

            return Ok(result);
        }

        [HttpPost("send-ping")]
        public ActionResult<bool> SendPing([FromBody] PingToClock pingToClock)
        {
            var result = _service.ExecuteProcess<PingToClock, bool>(pingToClock);

            return Ok(result);
        }

        [HttpPost("sync-clock-user-to-bd/{clockId}")]
        public async Task<ActionResult<bool>> SyncClockUserToBD(string clockId)
        {
            var result = await _service.ExecuteProcess<SyncClockUserToDB, Task<bool>>(new SyncClockUserToDB() { Id = Guid.Parse(clockId) });

            return Ok(result);
        }

        [HttpPost("sync-clock-attendance/{clockId}")]
        public async Task<ActionResult<bool>> SyncClockAttendace(string clockId)
        {
            var result = await _service.ExecuteProcess<SyncClockAttendance, Task<bool>>(new SyncClockAttendance()
            {
                Id = Guid.Parse(clockId),
            });

            return Ok(result);
        }

        [HttpPost("import-checkins-from-file")]
        public async Task<ActionResult<ResultImportCheckinsFromFile>> ImportCheckinsFromFile([FromForm] ImportCheckinsFromFile importCheckinsFromFile)
        {
            var result = await _service.ExecuteProcess<ImportCheckinsFromFile, Task<ResultImportCheckinsFromFile>>(importCheckinsFromFile);

            return Ok(result);
        }
    }
}
