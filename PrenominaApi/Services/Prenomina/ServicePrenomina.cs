using PrenominaApi.Models.Dto;
using PrenominaApi.Repositories.Prenomina;
using System.Reflection;

namespace PrenominaApi.Services.Prenomina
{
    public class ServicePrenomina<TEntity> : IBaseServicePrenomina<TEntity> where TEntity : class
    {
        protected IBaseRepositoryPrenomina<TEntity> _repository;

        public ServicePrenomina(IBaseRepositoryPrenomina<TEntity> respository)
        {
            _repository = respository;
        }

        public TObjectOutput ExecuteProcess<TObjectInput, TObjectOutput>(TObjectInput objectInput)
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
            catch (Exception ex) {
                throw new Exception(ex.InnerException?.Message ?? ex.Message, ex.InnerException ?? ex);
            }
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            return _repository.GetAll();
        }

        public IEnumerable<TEntity> GetByFilter(Func<TEntity, bool> predicate)
        {
            return _repository.GetByFilter(predicate);
        }

        public TEntity? GetById(string id)
        {
            return _repository.GetById(id);
        }

        public PagedResult<TEntity> GetWithPagination(int page, int pageSize, Func<TEntity, bool>? predicate)
        {
            return _repository.GetWithPagination(page, pageSize, predicate);
        }
    }
}
