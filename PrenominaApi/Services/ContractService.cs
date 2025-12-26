using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Services.Utilities.ContractPdf;

namespace PrenominaApi.Services
{
    public class ContractService : Service<Contract>
    {
        private readonly IBaseRepository<Employee> _employeeRepository;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IBaseRepository<Tabulator> _tabulatorRepository;
        private readonly IBaseRepositoryPrenomina<RehiredEmployees> _rehiredEmployee;
        private readonly IBaseRepository<Company> _companiesRepository;
        private readonly IBaseServicePrenomina<User> _userService;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly ContractPdfService _pdfService;

        public ContractService(
            IBaseRepository<Contract> repository,
            IBaseRepository<Employee> employeeRepository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Tabulator> tabulatorRepository,
            IBaseRepositoryPrenomina<RehiredEmployees> rehiredEmployee,
            IBaseRepository<Company> companiesRepository,
            IBaseServicePrenomina<User> userService,
            GlobalPropertyService globalPropertyService,
            ContractPdfService pdfService
        ) : base(repository) {
            _employeeRepository = employeeRepository;
            _keyRepository = keyRepository;
            _tabulatorRepository = tabulatorRepository;
            _rehiredEmployee = rehiredEmployee;
            _globalPropertyService = globalPropertyService;
            _pdfService = pdfService;
            _companiesRepository = companiesRepository;
            _userService = userService;
        }

        public IEnumerable<ContractsOutput> ExecuteProcess(ContractsInput contractsInput)
        {
            var userDetails = _userService.ExecuteProcess<string, UserDetails>(contractsInput.UserId!);
            var centers = userDetails.Centers?.Select(c => c.Id.Trim()).ToArray() ?? [];
            var supervisors = userDetails.Supervisors?.Select(s => s.Id).ToArray() ?? [];

            var keys = _keyRepository.GetContextEntity().Include(k => k.CenterItem).Where(
                item => item.Company == contractsInput.CompanyId &&
                contractsInput.Tenant != "all" ? 
                (contractsInput.TypeTenant == TypeTenant.Department ? item.Center.Trim() == contractsInput.Tenant : item.Supervisor == int.Parse(contractsInput.Tenant)) 
                : true
            ).AsEnumerable().Where(item => userDetails!.role!.Code == RoleCode.Sudo ? true :
                (contractsInput.TypeTenant == TypeTenant.Department ? (centers.Any() && centers.Contains(item.Center.Trim())) : (supervisors.Any() && supervisors.Contains(item.Supervisor)))).ToDictionary(k => (k.Codigo, k.Company));

            var employeeCodes = keys.Keys.Select(k => k.Codigo).ToHashSet();
            var ocupations = keys.Values.Select(k => k.Ocupation).ToHashSet();

            var rehiredEmployees = _rehiredEmployee.GetByFilter(item => item.CompanyId == contractsInput.CompanyId && employeeCodes.Contains(item.EmployeeCode)).ToList();
            var rehiredEmployee = rehiredEmployees.ToDictionary(re => (re.CompanyId, re.EmployeeCode, re.ContractFolio));

            if (contractsInput.IgnoreNotAction == true)
            {
                var listRehiredEmployees = rehiredEmployees.Select(re => re.EmployeeCode).ToHashSet();
                employeeCodes = keys.Keys.Where(k => listRehiredEmployees.Contains((int)k.Codigo)).Select(k => k.Codigo).ToHashSet();
            }

            var tabulators = _tabulatorRepository.GetByFilter(
                item => item.Company == contractsInput.CompanyId &&
                ocupations.Contains(item.Ocupation)
            ).ToDictionary(t => t.Ocupation, t => t.Activity);

            var contracts = _repository.GetByFilter(
                item => item.Company == contractsInput.CompanyId &&
                item.Days != 9999 &&
                employeeCodes.Contains(item.Codigo)
            ).GroupBy(c => (c.Codigo, c.Company)).ToDictionary(g => g.Key, g => g.ToList());

            var employees = _employeeRepository.GetByFilter(
                item => item.Company == contractsInput.CompanyId &&
                item.Active == 'S' && employeeCodes.Contains(item.Codigo)
            );

            var today = DateTime.Today;

            var result = employees.Select(employee =>
            {
                keys.TryGetValue((employee.Codigo, employee.Company), out var key);
                contracts.TryGetValue((employee.Codigo, employee.Company), out var contractsByUser);

                var ocupation = key?.Ocupation;
                var activity = ocupation != null && tabulators.TryGetValue((int)ocupation, out var act) ? act : null;
                var schedule = key?.Schedule;

                var lastStartDate = contractsByUser?.MaxBy(c => c.StartDate)?.StartDate;
                var folio = contractsByUser?.MaxBy(c => c.StartDate)?.Folio ?? 1;
                var lastTerminationDate = contractsByUser?.MaxBy(c => c.TerminationDate)?.TerminationDate;
                double expireInDays = lastTerminationDate.HasValue ? (lastTerminationDate.Value - today).TotalDays : 0;

                rehiredEmployee.TryGetValue((employee.Company, (int)employee.Codigo, folio), out var rehiredEmploye);

                return new ContractsOutput()
                {
                    Codigo = employee.Codigo,
                    Company = employee.Company,
                    Folio = folio,
                    LastName = employee.LastName,
                    MLastName = employee.MLastName,
                    Name = employee.Name,
                    Ocupation = ocupation,
                    Activity = activity,
                    Schedule = schedule,
                    SeniorityDate = employee.SeniorityDate,
                    StartDate = lastStartDate,
                    TerminationDate = lastTerminationDate,
                    Days = contractsByUser?.Sum(c => c.Days) ?? 0,
                    ExpireInDays = (int)expireInDays,
                    Observation = rehiredEmploye?.Observation,
                    ApplyRehired = rehiredEmploye?.ApplyRehired,
                    TenantName = key?.CenterItem?.DepartmentName,
                    ContractDays = rehiredEmploye?.ContractDays ?? 0
                };
            });

            return result.Where((item) => item.Folio != -1);
        }

        public byte[] ExecuteProcess(DownloadContracts downloadContracts)
        {
            var filter = new ContractsInput()
            {
                CompanyId = downloadContracts.Company,
                Tenant = "all",
                TypeNom = 1,
                TypeTenant = _globalPropertyService.TypeTenant,
                IgnoreNotAction = true,
                UserId = downloadContracts.UserId
            };

            var company = _companiesRepository.GetById(downloadContracts.Company);
            var result = this.ExecuteProcess<ContractsInput, IEnumerable<ContractsOutput>>(filter);

            return _pdfService.Generate(result, company!.Name, company!.RFC);
        }
        public ContractsOutput ExecuteProcess(SetApplyNewContract setApplyNewContract)
        {
            var today = DateTime.Today;
            var findContract = _rehiredEmployee.GetByFilter(c => c.ContractFolio == setApplyNewContract.Folio && c.EmployeeCode == setApplyNewContract.Codigo && c.CompanyId == setApplyNewContract.Company).FirstOrDefault();

            if (findContract == null)
            {
                _rehiredEmployee.Create(new RehiredEmployees() {
                    EmployeeCode = setApplyNewContract.Codigo,
                    ApplyRehired = setApplyNewContract.GenerateContract,
                    CompanyId = setApplyNewContract.Company,
                    ContractFolio = setApplyNewContract.Folio,
                    Observation = setApplyNewContract.Observation,
                    ContractDays = setApplyNewContract.ContractDays
                });
            } else
            {
                findContract.ApplyRehired = setApplyNewContract.GenerateContract;
                findContract.Observation = !setApplyNewContract.GenerateContract ? "" : setApplyNewContract.Observation;
                findContract.ContractDays = setApplyNewContract.ContractDays;

                _rehiredEmployee.Update(findContract);
            }

            _rehiredEmployee.Save();

            var keys = _keyRepository.GetByFilter(
                item => item.Company == setApplyNewContract.Company &&
                item.Codigo == setApplyNewContract.Codigo
            ).ToDictionary(k => (k.Codigo, k.Company));

            var ocupations = keys.Values.Select(k => k.Ocupation).ToHashSet();

            var tabulators = _tabulatorRepository.GetByFilter(
                item => item.Company == setApplyNewContract.Company &&
                ocupations.Contains(item.Ocupation)
            ).ToDictionary(t => t.Ocupation, t => t.Activity);

            var contracts = _repository.GetByFilter(
                item => item.Company == setApplyNewContract.Company &&
                item.Codigo == setApplyNewContract.Codigo &&
                item.Folio == setApplyNewContract.Folio
            ).GroupBy(c => (c.Codigo, c.Company)).ToDictionary(g => g.Key, g => g.ToList());

            var employee = _employeeRepository.GetByFilter(
                item => item.Company == setApplyNewContract.Company &&
                item.Active == 'S' && item.Codigo == setApplyNewContract.Codigo
            ).First();

            keys.TryGetValue((employee.Codigo, employee.Company), out var key);
            contracts.TryGetValue((employee.Codigo, employee.Company), out var contractsByUser);
            var ocupation = key?.Ocupation;
            var activity = ocupation != null && tabulators.TryGetValue((int)ocupation, out var act) ? act : null;
            var schedule = key?.Schedule;

            var lastStartDate = contractsByUser?.MaxBy(c => c.StartDate)?.StartDate;
            var lastTerminationDate = contractsByUser?.MaxBy(c => c.TerminationDate)?.TerminationDate;
            double expireInDays = lastTerminationDate.HasValue ? (lastTerminationDate.Value - today).TotalDays : 0;

            return new ContractsOutput()
            {
                Codigo = employee.Codigo,
                Company = employee.Company,
                Folio = setApplyNewContract.Folio,
                LastName = employee.LastName,
                MLastName = employee.LastName,
                Name = employee.Name,
                Ocupation = ocupation,
                Activity = activity,
                Schedule = schedule,
                SeniorityDate = employee.SeniorityDate,
                StartDate = lastStartDate,
                TerminationDate = lastTerminationDate,
                Days = contractsByUser?.Sum(c => c.Days) ?? 0,
                ExpireInDays = (int)expireInDays,
                Observation = setApplyNewContract.Observation,
                ApplyRehired = setApplyNewContract.GenerateContract,
                ContractDays = setApplyNewContract.ContractDays
            };
        }
    }
}
