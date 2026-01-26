using PrenominaApi.Models.Dto;
using PrenominaApi.Repositories;
using System.Reflection;

namespace PrenominaApi.Services
{
    public class Service<TEntity> : IBaseService<TEntity> where TEntity : class
    {
        protected IBaseRepository<TEntity> _repository;

        public Service(IBaseRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public virtual TObjectOutput ExecuteProcess<TObjectInput, TObjectOutput>(TObjectInput objectInput)
        {
            MethodInfo? method = GetType().GetMethod("ExecuteProcess", new Type[1] { typeof(TObjectInput) });

            if (method is null)
            {
                throw new ArgumentException("Method No Implement!");
            }

            try
            {
                MethodInfo methodExecute = method;
                object? result = methodExecute.Invoke(this, new object[] { objectInput! });

                if (result is null)
                {
                    throw new InvalidOperationException("El método ejecutado devolvió un valor nulo.");
                }

                return (TObjectOutput)result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException?.Message ?? ex.Message, ex.InnerException ?? ex);
            }
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            return _repository.GetAll();
        }

        public virtual PagedResult<TEntity> GetWithPagination(int page, int pageSize, Func<TEntity, bool>? predicate)
        {
            return _repository.GetWithPagination(page, pageSize, predicate);
        }

        public virtual Task<TEntity> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> GetByFilter(Func<TEntity, bool> predicate)
        {
            return _repository.GetByFilter(predicate);
        }
    }
}
