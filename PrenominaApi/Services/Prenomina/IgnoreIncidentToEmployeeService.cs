using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class IgnoreIncidentToEmployeeService : ServicePrenomina<IgnoreIncidentToEmployee>
    {

        public IgnoreIncidentToEmployeeService(IBaseRepositoryPrenomina<IgnoreIncidentToEmployee> baseRepository) : base(baseRepository) { }

        public bool ExecuteProcess(AddIgnoreIncidentToEmployee addIgnoreIncidentToEmployee)
        {
            foreach (var incidentCode in addIgnoreIncidentToEmployee.IncidentCodes)
            {
                var exist = _repository.GetByFilter((item) => item.IncidentCode == incidentCode.Code && item.EmployeeCode == addIgnoreIncidentToEmployee.EmployeeCode).FirstOrDefault();

                if (exist != null) {
                    exist.Ignore = incidentCode.Ignore;
                    _repository.Update(exist);
                } else
                {
                    _repository.Create(new IgnoreIncidentToEmployee()
                    {
                        IncidentCode = incidentCode.Code,
                        EmployeeCode = addIgnoreIncidentToEmployee.EmployeeCode,
                        Ignore = incidentCode.Ignore,
                    });
                }
            }

            _repository.Save();

            return true;
        }
    }
}
