using PrenominaApi.Models.Dto;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;

namespace PrenominaApi.Repositories.Prenomina
{
    public interface IBaseRepositoryPrenomina<T> where T : class
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> GetByFilter(Func<T, bool> predicate);
        PagedResult<T> GetWithPagination(int page, int pageSize, Func<T, bool>? predicate);
        T? GetById(object id);
        T Create(T entity);
        T Update(T entity);
        DbSet<T> GetContextEntity();
        T Delete(T entity);
        void Save();
        PrenominaDbContext GetDbContext();
    }
}
