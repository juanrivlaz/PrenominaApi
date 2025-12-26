using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto;

namespace PrenominaApi.Repositories.Prenomina
{
    public class RepositoryPrenomina<TEntity> : IBaseRepositoryPrenomina<TEntity> where TEntity : class
    {
        private readonly PrenominaDbContext _context;
        public RepositoryPrenomina(PrenominaDbContext context) {
            _context = context;
        }

        public TEntity Create(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);

            return entity;
        }

        public TEntity Delete(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);

            return entity;
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _context.Set<TEntity>().ToList();
        }

        public IEnumerable<TEntity> GetByFilter(Func<TEntity, bool> predicate)
        {
            return _context.Set<TEntity>().Where(predicate).ToList();
        }

        public TEntity? GetById(object id)
        {
            return _context.Set<TEntity>().Find(id);
        }

        public PagedResult<TEntity> GetWithPagination(int page, int pageSize, Func<TEntity, bool>? predicate)
        {
            var entity = _context.Set<TEntity>();
            int totalRecords = predicate is not null ? entity.Where(predicate).Count() : entity.Count();
            var items = (predicate is not null ? entity.Where(predicate) : entity).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return new PagedResult<TEntity>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
            };
        }

        public TEntity Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);

            return entity;
        }

        public DbSet<TEntity> GetContextEntity()
        {
            return _context.Set<TEntity>();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public PrenominaDbContext GetDbContext()
        {
            return _context;
        }
    }
}
