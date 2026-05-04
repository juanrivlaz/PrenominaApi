using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input.Documents;
using PrenominaApi.Models.Dto.Output.Documents;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _service;

        public DocumentsController(DocumentService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<List<DocumentOutput>> List()
        {
            return Ok(_service.List());
        }

        [HttpGet("{id}")]
        public ActionResult<DocumentOutput> GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<DocumentOutput> Create([FromBody] DocumentInput input)
        {
            return Ok(_service.Create(input));
        }

        [HttpPut("{id}")]
        public ActionResult<bool> Update(Guid id, [FromBody] DocumentInput input)
        {
            var ok = _service.Update(id, input);
            if (!ok) return NotFound();
            return Ok(true);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete(Guid id)
        {
            var ok = _service.Delete(id);
            if (!ok) return NotFound();
            return Ok(true);
        }
    }
}
