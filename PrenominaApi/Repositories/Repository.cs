using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto;

namespace PrenominaApi.Repositories
{
    public class Repository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        public readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public TEntity Create(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);

            return entity;
        }

        public Task Delete(TEntity entity)
        {
            throw new NotImplementedException();
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

        public Task<TEntity> Update(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public DbSet<TEntity> GetContextEntity()
        {
            return _context.Set<TEntity>();
        }

        public ApplicationDbContext GetDbContext()
        {
            return _context;
        }
    }
}
