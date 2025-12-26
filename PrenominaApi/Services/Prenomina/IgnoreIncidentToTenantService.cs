using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Services.Prenomina
{
    public class IgnoreIncidentToTenantService : ServicePrenomina<IgnoreIncidentToTenant>
    {
        private readonly IBaseRepository<Models.Company> _companyRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRespository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        public IgnoreIncidentToTenantService(
            IBaseRepositoryPrenomina<IgnoreIncidentToTenant> baseRepository,
            IBaseRepository<Models.Company> companyRepository,
            IBaseRepository<Supervisor> supervisorRespository,
            IBaseRepository<Center> centerRepository,
            GlobalPropertyService globalPropertyService
        ) : base(baseRepository)
        {
            _globalPropertyService = globalPropertyService;
            _companyRepository = companyRepository;
            _supervisorRespository = supervisorRespository;
            _centerRepository = centerRepository;
        }

        public TenantsForIgnoreIncident ExecuteProcess(int companyId)
        {
            var company = _companyRepository.GetByFilter(c => c.Id == companyId).FirstOrDefault();
            var typeSystem = _globalPropertyService.TypeTenant;
            var result = new TenantsForIgnoreIncident();
            result.TypeTenant = typeSystem;

            if (company == null) {
                return result;
            }

            if (typeSystem == TypeTenant.Supervisor)
            {
                var supervisors = _supervisorRespository.GetByFilter(s => s.Company == company.Id);

                result.Supervisors = supervisors;
            } else
            {
                var centers = _centerRepository.GetContextEntity().Include(c => c.Keys).Where(c => c.Keys != null && c.Keys.Any() && c.Company == company.Id);

                result.Centers = centers;
            }

            return result;
        }

        public bool ExecuteProcess(AddIgnoreIncidentToTenant addIgnoreIncidentToTenant)
        {
            var typeSystem = _globalPropertyService.TypeTenant;

            foreach (var incidentCode in addIgnoreIncidentToTenant.IncidentCodes)
            {
                var exist = _repository.GetByFilter((item) => item.IncidentCode == incidentCode.Code && typeSystem == TypeTenant.Department ? item.DepartmentCode == addIgnoreIncidentToTenant.TenantId : item.SupervisorId == int.Parse(addIgnoreIncidentToTenant.TenantId)).FirstOrDefault();

                if (exist != null)
                {
                    exist.Ignore = incidentCode.Ignore;
                    _repository.Update(exist);
                } else
                {
                    _repository.Create(new IgnoreIncidentToTenant()
                    {
                        IncidentCode = incidentCode.Code,
                        Ignore = incidentCode.Ignore,
                        DepartmentCode = typeSystem == TypeTenant.Department ? addIgnoreIncidentToTenant.TenantId : null,
                        SupervisorId = typeSystem == TypeTenant.Supervisor ? int.Parse(addIgnoreIncidentToTenant.TenantId) : null
                    });
                }
            }

            _repository.Save();

            return true;
        }
    }
}
