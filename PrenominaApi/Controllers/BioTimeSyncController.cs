using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.BioTime;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class BioTimeSyncController : ControllerBase
    {
        private readonly BioTimeSyncService _service;

        public BioTimeSyncController(BioTimeSyncService service)
        {
            _service = service;
        }

        [HttpGet("config")]
        public async Task<ActionResult> GetConfig()
        {
            var config = await _service.GetSyncConfig();
            return Ok(config ?? new SysBioTimeSyncConfig());
        }

        [HttpPut("config")]
        public async Task<ActionResult> SaveConfig([FromBody] SysBioTimeSyncConfig config)
        {
            await _service.SaveSyncConfig(config);
            return Ok(new { message = "Configuración guardada" });
        }

        [HttpPut("credentials")]
        public async Task<ActionResult> SaveCredentials([FromBody] BioTimeCredentials credentials)
        {
            await _service.SaveCredentials(credentials);
            return Ok(new { message = "Credenciales guardadas de forma segura" });
        }

        [HttpGet("credentials/status")]
        public async Task<ActionResult> GetCredentialsStatus()
        {
            var credentials = await _service.GetCredentials();
            return Ok(new
            {
                configured = credentials != null,
                email = credentials?.Email ?? "",
                company = credentials?.Company ?? ""
            });
        }

        [HttpPost("sync-now")]
        public async Task<ActionResult> SyncNow()
        {
            var result = await _service.SyncYesterdayAttendance();
            return result
                ? Ok(new { message = "Sincronización completada" })
                : BadRequest(new { message = "Error en la sincronización. Revise los logs." });
        }
    }
}
