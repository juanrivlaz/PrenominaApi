using PrenominaApi.Models.Dto;

namespace PrenominaApi.Services.Prenomina
{
    public interface IBaseServicePrenomina<T>
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> GetByFilter(Func<T, bool> predicate);
        PagedResult<T> GetWithPagination(int page, int pageSize, Func<T, bool>? predicate);
        T? GetById(string id);
        TObjectOutput ExecuteProcess<TObjectInput, TObjectOutput>(TObjectInput objectInput);
    }
}
