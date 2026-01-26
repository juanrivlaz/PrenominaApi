using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Services.Utilities.PermissionPdf;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly PermissionPdfService _permissionPdfService;

        public PermissionController(PermissionPdfService permissionPdfService)
        {
            _permissionPdfService = permissionPdfService;
        }

        [HttpGet("download")]
        public IActionResult Download()
        {
            var pdfBytes = _permissionPdfService.Generate("CELACANTO SERVICIOS TURISTICOS S DE RL DE CV", "HUMBERTO HERNANDEZ GOMEZ", "93", "CANTINERO", "CANTINEROS", "25/01/2026", "Permiso Sin Goce", "salida familiar", "25/01/2026", "28/01/2026", "4");
            return File(pdfBytes, "application/pdf", "Reporte_Permisos.pdf");
        }
    }
}
