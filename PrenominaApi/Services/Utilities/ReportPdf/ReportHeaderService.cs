using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Services.Utilities.ReportPdf
{
    /// <summary>
    /// Resuelve los datos del encabezado de los reportes PDF (empresa, RFC, tenant, tipo de
    /// nómina y periodo) reutilizando la misma lógica del reporte de asistencia.
    /// </summary>
    public class ReportHeaderService
    {
        private readonly IBaseRepository<Company> _companiesRepository;
        private readonly IBaseRepository<Payroll> _payrollRepository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRepository;
        private readonly IBaseServicePrenomina<Models.Prenomina.Period> _periodRepository;
        private readonly GlobalPropertyService _globalPropertyService;

        public ReportHeaderService(
            IBaseRepository<Company> companiesRepository,
            IBaseRepository<Payroll> payrollRepository,
            IBaseRepository<Center> centerRepository,
            IBaseRepository<Supervisor> supervisorRepository,
            IBaseServicePrenomina<Models.Prenomina.Period> periodRepository,
            GlobalPropertyService globalPropertyService
        )
        {
            _companiesRepository = companiesRepository;
            _payrollRepository = payrollRepository;
            _centerRepository = centerRepository;
            _supervisorRepository = supervisorRepository;
            _periodRepository = periodRepository;
            _globalPropertyService = globalPropertyService;
        }

        public ReportPdfHeaderContext Build(decimal company, string tenant, int typeNomina, int numPeriod)
        {
            var companyEntity = _companiesRepository.GetById(company);
            var year = _globalPropertyService.YearOfOperation;

            var period = _periodRepository.GetByFilter(
                p => p.TypePayroll == typeNomina &&
                     p.Company == (int)company &&
                     p.NumPeriod == numPeriod &&
                     p.Year == year
            ).FirstOrDefault();

            var payroll = _payrollRepository.GetByFilter(
                p => p.Company == company && p.TypeNom == typeNomina
            ).FirstOrDefault();

            var tenantName = "Todos";
            if (!string.IsNullOrWhiteSpace(tenant) && tenant != "-999" && companyEntity != null)
            {
                tenantName = _globalPropertyService.TypeTenant == TypeTenant.Department
                    ? _centerRepository.GetByFilter(c => c.Company == companyEntity.Id)
                        .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Id) && int.TryParse(c.Id.Trim(), out var cId) && cId == int.Parse(tenant))?.DepartmentName ?? ""
                    : _supervisorRepository.GetByFilter(s => s.Id == Convert.ToDecimal(tenant))
                        .FirstOrDefault()?.Name ?? "";
            }

            return new ReportPdfHeaderContext
            {
                CompanyName = companyEntity?.Name ?? "",
                RfcInfo = companyEntity != null
                    ? $"RFC: {companyEntity.RFC} | R. Patronal: {companyEntity.EmployerRegistration}"
                    : "",
                TenantName = tenantName,
                TypeNom = payroll != null ? $"{payroll.TypeNom} - {payroll.Label}" : typeNomina.ToString(),
                Period = period != null
                    ? $"{period.StartDate:dd/MM/yyyy} - {period.ClosingDate:dd/MM/yyyy}"
                    : numPeriod.ToString()
            };
        }
    }
}
