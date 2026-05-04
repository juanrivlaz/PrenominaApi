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
            return _context.documents
                .AsNoTracking()
                .Where(d => d.DeletedAt == null)
                .OrderBy(d => d.Name)
                .Select(d => new DocumentOutput
                {
                    Id = d.Id,
                    Name = d.Name,
                    Path = d.Path,
                    Content = d.Content,
                    Module = d.Module,
                    KeyParams = d.KeyParams
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
                KeyParams = d.KeyParams
            };
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
