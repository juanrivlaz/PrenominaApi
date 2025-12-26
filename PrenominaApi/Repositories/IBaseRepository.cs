using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto;

namespace PrenominaApi.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> GetByFilter(Func<T, bool> predicate);
        PagedResult<T> GetWithPagination(int page, int pageSize, Func<T, bool>? predicate);
        T? GetById(object id);
        T Create(T entity);
        Task<T> Update(T entity);
        DbSet<T> GetContextEntity();
        Task Delete(T entity);
        ApplicationDbContext GetDbContext();
    }
}
