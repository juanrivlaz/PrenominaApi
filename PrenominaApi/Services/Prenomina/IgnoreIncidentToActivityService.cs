using PrenominaApi.Models;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class IgnoreIncidentToActivityService : ServicePrenomina<IgnoreIncidentToActivity>
    {
        private readonly IBaseRepository<Tabulator> _tabulatorRepository;
        public IgnoreIncidentToActivityService(
            IBaseRepositoryPrenomina<IgnoreIncidentToActivity> baseRepository,
            IBaseRepository<Tabulator> tabulatorRepository
        ) : base (baseRepository) {
            _tabulatorRepository = tabulatorRepository;
        }

        public bool ExecuteProcess(AddIgnoreIncidentToActivity addIgnoreIncidentToActivity)
        {
            foreach (var incidentCode in addIgnoreIncidentToActivity.IncidentCodes)
            {
                var exist = _repository.GetByFilter((item) => item.IncidentCode == incidentCode.Code && item.ActivityId == addIgnoreIncidentToActivity.ActivityId).FirstOrDefault();

                if (exist != null) {
                    exist.Ignore = incidentCode.Ignore;
                    _repository.Update(exist);
                } else
                {
                    _repository.Create(new IgnoreIncidentToActivity()
                    {
                        IncidentCode = incidentCode.Code,
                        ActivityId = addIgnoreIncidentToActivity.ActivityId,
                        Ignore = incidentCode.Ignore,
                    });
                }
            }

            _repository.Save();

            return true;
        }

        public IEnumerable<Tabulator> ExecuteProcess(int companyId)
        {
            var tabulators = _tabulatorRepository.GetByFilter((tabulator) => tabulator.Company == companyId);

            return tabulators;
        }
    }
}
