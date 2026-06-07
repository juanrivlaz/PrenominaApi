using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto.Input.Documents;
using PrenominaApi.Models.Dto.Output.Documents;
using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class DocumentService
    {
        private readonly PrenominaDbContext _context;

        public DocumentService(PrenominaDbContext context)
        {
            _context = context;
        }

        public List<DocumentOutput> List()
        {
            var documents = _context.documents
                .AsNoTracking()
                .Where(d => d.DeletedAt == null)
                .OrderBy(d => d.Name)
                .ToList();

            if (documents.Count == 0)
            {
                return new List<DocumentOutput>();
            }

            // Firmantes (roles de la cadena de firmas) por documento, en orden.
            var docIds = documents.Select(d => d.Id).ToList();
            var steps = _context.documentApprovalSteps
                .AsNoTracking()
                .Where(s => docIds.Contains(s.DocumentId))
                .OrderBy(s => s.StepOrder)
                .ToList();
            var roleIds = steps.Select(s => s.RoleId).Distinct().ToList();
            var roleLabels = _context.roles.AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionary(r => r.Id, r => r.Label);
            var signersByDoc = steps
                .GroupBy(s => s.DocumentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => roleLabels.TryGetValue(s.RoleId, out var label) ? label : "Rol").ToList());

            return documents
                .Select(d => new DocumentOutput
                {
                    Id = d.Id,
                    Name = d.Name,
                    Path = d.Path,
                    Content = d.Content,
                    Module = d.Module,
                    KeyParams = d.KeyParams,
                    Signers = signersByDoc.TryGetValue(d.Id, out var signers) ? signers : new List<string>()
                })
                .ToList();
        }

        public DocumentOutput? GetById(Guid id)
        {
            var d = _context.documents.AsNoTracking().FirstOrDefault(x => x.Id == id && x.DeletedAt == null);
            if (d == null) return null;

            return new DocumentOutput
            {
                Id = d.Id,
                Name = d.Name,
                Path = d.Path,
                Content = d.Content,
                Module = d.Module,
                KeyParams = d.KeyParams,
                ApprovalSteps = _context.documentApprovalSteps
                    .AsNoTracking()
                    .Where(s => s.DocumentId == d.Id)
                    .OrderBy(s => s.StepOrder)
                    .Select(s => new DocumentApprovalStepOutput
                    {
                        StepOrder = s.StepOrder,
                        RoleId = s.RoleId,
                        Scope = (int)s.Scope,
                        Mode = (int)s.Mode,
                        IsOptional = s.IsOptional,
                    })
                    .ToList()
            };
        }

        // Reemplaza la cadena de firmas de un documento por la enviada (orden por posición).
        private void ReplaceApprovalSteps(Guid documentId, List<Models.Dto.Input.ApprovalStepInput>? steps)
        {
            var existing = _context.documentApprovalSteps.Where(s => s.DocumentId == documentId).ToList();
            _context.documentApprovalSteps.RemoveRange(existing);

            if (steps != null)
            {
                var order = 1;
                foreach (var step in steps.OrderBy(s => s.StepOrder))
                {
                    _context.documentApprovalSteps.Add(new DocumentApprovalStep
                    {
                        DocumentId = documentId,
                        StepOrder = order++,
                        RoleId = step.RoleId,
                        Scope = step.Scope,
                        Mode = step.Mode,
                        IsOptional = step.IsOptional,
                    });
                }
            }

            _context.SaveChanges();
        }

        public DocumentOutput Create(DocumentInput dto)
        {
            var existing = _context.documents.FirstOrDefault(d => d.Name == dto.Name && d.DeletedAt == null);
            if (existing != null)
            {
                throw new BadHttpRequestException("Ya existe un documento con ese nombre");
            }

            var entity = new Document
            {
                Name = dto.Name,
                Path = dto.Path,
                Content = dto.Content,
                Module = dto.Module,
                KeyParams = dto.KeyParams
            };

            _context.documents.Add(entity);
            _context.SaveChanges();

            ReplaceApprovalSteps(entity.Id, dto.ApprovalSteps);

            return new DocumentOutput
            {
                Id = entity.Id,
                Name = entity.Name,
                Path = entity.Path,
                Content = entity.Content,
                Module = entity.Module,
                KeyParams = entity.KeyParams
            };
        }

        public bool Update(Guid id, DocumentInput dto)
        {
            var entity = _context.documents.FirstOrDefault(d => d.Id == id && d.DeletedAt == null);
            if (entity == null) return false;

            var nameClash = _context.documents.FirstOrDefault(d => d.Name == dto.Name && d.Id != id && d.DeletedAt == null);
            if (nameClash != null)
            {
                throw new BadHttpRequestException("Ya existe otro documento con ese nombre");
            }

            entity.Name = dto.Name;
            entity.Path = dto.Path;
            entity.Content = dto.Content;
            entity.Module = dto.Module;
            entity.KeyParams = dto.KeyParams;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            ReplaceApprovalSteps(entity.Id, dto.ApprovalSteps);
            return true;
        }

        public bool Delete(Guid id)
        {
            var entity = _context.documents.FirstOrDefault(d => d.Id == id && d.DeletedAt == null);
            if (entity == null) return false;

            entity.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();
            return true;
        }
    }
}
