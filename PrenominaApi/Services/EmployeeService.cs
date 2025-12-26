using Microsoft.AspNetCore.Identity;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services
{
    public class EmployeeService : Service<Employee>
    {
        private readonly IBaseRepositoryPrenomina<User> _repositoryPrenomina;
        private readonly IPasswordHasher<HasPassword> _passwordHasher;
        public EmployeeService(
            IBaseRepository<Employee> repository,
            IBaseRepositoryPrenomina<User> repositoryPrenomina,
            IPasswordHasher<HasPassword> passwordHasher
        ) : base(repository) {
            _repositoryPrenomina = repositoryPrenomina;
            _passwordHasher = passwordHasher;
        }

        public override IEnumerable<Employee> GetAll()
        {
            var result = _repository.GetAll();
            return result.OrderBy(item => item.Codigo);
        }

        public bool ExecuteProcess(int id)
        {
            return id == 0;
        }
    }
}
