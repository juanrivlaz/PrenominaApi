using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.EmployeeAbsenceRequest;
using PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Utilities.PermissionPdf;

namespace PrenominaApi.Services.Prenomina
{
    public class EmployeeAbsenceRequestsService : ServicePrenomina<EmployeeAbsenceRequests>
    {
        public readonly IBaseRepository<Key> _keyRepository;
        public readonly IBaseRepository<Company> _companyRepository;
        private readonly PermissionPdfService _permissionPdfService;
        public readonly GlobalPropertyService _globalPropertyService;

        public EmployeeAbsenceRequestsService(
            IBaseRepositoryPrenomina<EmployeeAbsenceRequests> repository,
            IBaseRepository<Key> keyRepository,
            IBaseRepository<Company> companyRepository,
            GlobalPropertyService globalPropertyService,
            PermissionPdfService permissionPdfService
        ) : base(repository)
        {
            _keyRepository = keyRepository;
            _companyRepository = companyRepository;
            _globalPropertyService = globalPropertyService;
            _permissionPdfService = permissionPdfService;
        }

        public IEnumerable<EmployeeAbsenceRequestOutput> ExecuteProcess(decimal companyId)
        {
            var requests = _repository.GetContextEntity().Include(e => e.IncidentCodeItem).Where(e => e.CompanyId == companyId).ToList();
            var employeeCodes = requests.Select(r => r.EmployeeCode).Distinct().ToList();
            var keyEmployee = _keyRepository.GetContextEntity().Include(k => k.Tabulator).Include(k => k.Employee);

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                keyEmployee.Include(k => k.CenterItem);
            }
            else
            {
                keyEmployee.Include(k => k.SupervisorItem);
            }

            var keys = keyEmployee.Where(k => k.Company == companyId && employeeCodes.Contains((int)k.Codigo)).ToList();

            var result = requests.Select(r =>
            {
                var key = keys.FirstOrDefault(k => k.Codigo == r.EmployeeCode);
                var employee = key?.Employee;
                var activity = key?.Tabulator.Activity;
                var incident = r.IncidentCodeItem;

                return new EmployeeAbsenceRequestOutput
                {
                    Id = r.Id,
                    EmployeeName = $"{employee?.Name ?? string.Empty} {employee?.LastName ?? string.Empty} {employee?.MLastName ?? string.Empty}",
                    EmployeeCode = r.EmployeeCode,
                    EmployeeActivity = activity ?? string.Empty,
                    IncidentCode = r.IncidentCode,
                    IncidentDescription = incident?.Label ?? string.Empty,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    Notes = r.Notes,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                };
            });

            return result;
        }

        public bool ExecuteProcess(RegisterDaysOff registerDaysOff)
        {
            var firstDate = registerDaysOff.Dates.Min();
            var lastDate = registerDaysOff.Dates.Max();

            var company = _companyRepository.GetById(registerDaysOff.CompanyId);

            if (company == null)
            {
                throw new BadHttpRequestException("La empresa no existe");
            }

            var item = new EmployeeAbsenceRequests()
            {
                CompanyId = company.Id,
                EmployeeCode = (int)registerDaysOff.EmployeeCode,
                EndDate = lastDate,
                IncidentCode = registerDaysOff.IncidentCode,
                StartDate = firstDate,
                Notes = registerDaysOff.Notes,
                Status = AbsenceRequestStatus.Pending,
            };

            _repository.Create(item);
            _repository.Save();

            return true;
        }

        public bool ExecuteProcess(ChangeStatus changeStatus)
        {
            if (string.IsNullOrEmpty(changeStatus.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetById(Guid.Parse(changeStatus.Id));
            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            item.Status = changeStatus.Status;
            _repository.Update(item);
            _repository.Save();

            return true;
        }

        public byte[] ExecuteProcess(DownloadRequest downloadRequest)
        {
            if (string.IsNullOrEmpty(downloadRequest.Id))
            {
                throw new BadHttpRequestException("El Id de la solicitud de ausencia es requerido");
            }

            var item = _repository.GetContextEntity()
                .Include(e => e.IncidentCodeItem)
                .FirstOrDefault(e => e.Id == Guid.Parse(downloadRequest.Id));

            if (item == null)
            {
                throw new BadHttpRequestException("La solicitud de ausencia no existe");
            }

            if (item.Status != AbsenceRequestStatus.Approved)
            {
                throw new BadHttpRequestException("La solicitud a un no ha sido aprobada");
            }

            var keyEmployee = _keyRepository.GetContextEntity().Include(k => k.Tabulator).Include(k => k.CenterItem).Include(k => k.SupervisorItem).Include(k => k.Employee);
            var company = _companyRepository.GetById(item.CompanyId);

            if (company == null)
            {
                throw new BadHttpRequestException("La empresa no existe");
            }

            var keys = keyEmployee.Where(k => k.Company == company.Id && (int)k.Codigo == item.EmployeeCode).SingleOrDefault();

            var days = (item.EndDate.ToDateTime(TimeOnly.MinValue) - item.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

            return _permissionPdfService.Generate(
                company.Name, 
                $"{keys?.Employee.Name ?? string.Empty} {keys?.Employee.LastName ?? string.Empty} {keys?.Employee.MLastName ?? string.Empty}",
                $"{item.EmployeeCode}", 
                $"{keys?.Tabulator.Activity}",
                _globalPropertyService.TypeTenant == TypeTenant.Department ? keys?.CenterItem?.DepartmentName ?? string.Empty :
                keys?.SupervisorItem?.Name ?? string.Empty, 
                item.CreatedAt.ToString("dd/MM/yyyy"),
                item.IncidentCodeItem?.Label ?? string.Empty, 
                item.Notes ?? string.Empty, 
                item.StartDate.ToString("dd/MM/yyyy"), 
                item.EndDate.ToString("dd/MM/yyyy"), 
                days.ToString()
            );
        }
    }
}
