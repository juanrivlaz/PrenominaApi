using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    public class ActivitiesController : ControllerBase
    {
        private readonly PrenominaDbContext _context;

        public ActivitiesController(PrenominaDbContext context)
        {
            _context = context;
        }

        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return int.Parse(company);
        }

        [HttpGet("overtime-configs")]
        [Authorize]
        public async Task<ActionResult> GetOvertimeConfigs()
        {
            var companyId = GetCompanyId();

            var configs = await _context.activityOvertimeConfigs
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.ExcludeOvertime)
                .Select(c => c.ActivityId)
                .ToListAsync();

            return Ok(configs);
        }

        [HttpPut("{activityId}/exclude-overtime")]
        [Authorize]
        public async Task<ActionResult<bool>> UpdateExcludeOvertime(int activityId, [FromBody] ExcludeOvertimeInput input)
        {
            var companyId = GetCompanyId();

            var config = await _context.activityOvertimeConfigs
                .FirstOrDefaultAsync(c => c.ActivityId == activityId && c.CompanyId == companyId);

            if (config == null)
            {
                config = new ActivityOvertimeConfig
                {
                    ActivityId = activityId,
                    CompanyId = companyId,
                    ExcludeOvertime = input.ExcludeOvertime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.activityOvertimeConfigs.Add(config);
            }
            else
            {
                config.ExcludeOvertime = input.ExcludeOvertime;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(true);
        }
    }
}
