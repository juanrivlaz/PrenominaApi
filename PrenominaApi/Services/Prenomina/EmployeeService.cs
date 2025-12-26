using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;

namespace PrenominaApi.Services.Prenomina
{
    public class EmployeeService : Service<Employee>
    {
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        public EmployeeService(
            IBaseRepository<Employee> baseRepository,
            IBaseRepository<Key> keyRepository,
            GlobalPropertyService globalPropertyService) : base(baseRepository)
        {
            _globalPropertyService = globalPropertyService;
            _keyRepository = keyRepository;
        }

        public PagedResult<EmployeeOutput> ExecuteProcess(FilterEmployeesByPayroll filterEmployee)
        {
            List<Key> keys;

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filterEmployee.CompanyId && 
                    (filterEmployee.TypeNom >= 0 ? item.TypeNom == filterEmployee.TypeNom : true) && 
                    (filterEmployee.Tenant != "-999" ? item.Center.Trim() == filterEmployee.Tenant : true)
                ).Include(k => k.Tabulator).Include(k => k.CenterItem).ToList();
            }
            else
            {
                keys = _keyRepository.GetContextEntity().Where(
                    item => item.Company == filterEmployee.CompanyId && 
                    (filterEmployee.TypeNom >= 0 ? item.TypeNom == filterEmployee.TypeNom : true) &&
                    (filterEmployee.Tenant != "-999" ? item.Supervisor == Convert.ToDecimal(filterEmployee.Tenant) : true)
                ).Include(k => k.Tabulator).Include(k => k.SupervisorItem).ToList();
            }

            var employeeCodes = keys.Select(k => k.Codigo).ToList();

            Func<Employee, bool> filter = employee => employee.Company == filterEmployee.CompanyId && employeeCodes.Contains(employee.Codigo) && employee.Active == 'S';

            if (!string.IsNullOrWhiteSpace(filterEmployee.Search))
            {
                var searchTerm = filterEmployee.Search.ToLower();
                filter = employee =>
                    employeeCodes.Contains(employee.Codigo) && employee.Company == filterEmployee.CompanyId &&
                    ($"{employee.Name} {employee.LastName} {employee.MLastName}".ToLower().Contains(searchTerm));
            }

            if (filterEmployee.NoPagination != null && (bool)filterEmployee.NoPagination)
            {
                var employees = _repository.GetByFilter(filter).Select(e => {
                    var key = keys.FirstOrDefault(k => k.Codigo == e.Codigo);
                    return new EmployeeOutput
                    {
                        Active = e.Active,
                        Activity = key?.Tabulator.Activity ?? "",
                        LastName = e.LastName,
                        MLastName = e.MLastName,
                        Name = e.Name,
                        TenantName = key?.CenterItem?.DepartmentName ?? key?.SupervisorItem?.Name ?? "",
                        Codigo = e.Codigo,
                        Company = e.Company,
                        Salary = e.Salary,
                        SeniorityDate = e.SeniorityDate,
                    };
                });

                return new PagedResult<EmployeeOutput>() { Items = employees };
            }

            var result = _repository.GetWithPagination(filterEmployee.Page, filterEmployee.PageSize, filter);

            return new PagedResult<EmployeeOutput>() {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                TotalRecords = result.TotalRecords,
                Items = result.Items.Select(e =>
                {
                    var key = keys.FirstOrDefault(k => k.Codigo == e.Codigo);

                    return new EmployeeOutput
                    {
                        Active = e.Active,
                        Activity = key?.Tabulator.Activity ?? "",
                        LastName = e.LastName,
                        MLastName = e.MLastName,
                        Name = e.Name,
                        TenantName = key?.CenterItem?.DepartmentName ?? key?.SupervisorItem?.Name ?? "",
                        Codigo = e.Codigo,
                        Company = e.Company,
                        Salary = e.Salary,
                        SeniorityDate = e.SeniorityDate
                    };
                })
            };
        }
    }
}
