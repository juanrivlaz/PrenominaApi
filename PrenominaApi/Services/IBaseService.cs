using PrenominaApi.Models.Dto;

namespace PrenominaApi.Services
{
    public interface IBaseService<T>
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> GetByFilter(Func<T, bool> predicate);
        PagedResult<T> GetWithPagination(int page, int pageSize, Func<T, bool>? predicate);
        Task<T> GetById(int id);
        TObjectOutput ExecuteProcess<TObjectInput, TObjectOutput>(TObjectInput objectInput);
    }
}
