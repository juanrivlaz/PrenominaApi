using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class PeriodService : ServicePrenomina<Models.Prenomina.Period>
    {
        private readonly IBaseRepository<Payroll> _payrollRepository;
        private readonly IBaseRepositoryPrenomina<PeriodStatus> _periodStatusRepo;
        public PeriodService(
            IBaseRepositoryPrenomina<Models.Prenomina.Period> baseRepository,
            IBaseRepository<Payroll> payrollRepository,
            IBaseRepositoryPrenomina<PeriodStatus> periodStatusRepo
        ): base(baseRepository) {
            _payrollRepository = payrollRepository;
            _periodStatusRepo = periodStatusRepo;
        }

        public IEnumerable<Payroll> ExecuteProcess(int companyId)
        {
            var payrolls = _payrollRepository.GetByFilter((payroll) => payroll.Company == companyId);

            return payrolls;
        }

        public IEnumerable<Models.Prenomina.Period> ExecuteProcess(CreatePeriods createPeriods)
        {
            var result = new List<Models.Prenomina.Period>();
            var payroll = _payrollRepository.GetByFilter(p => p.TypeNom == createPeriods.TypePayroll && p.Company == createPeriods.CompanyId).First();

            if (payroll == null) {
                throw new BadHttpRequestException("El tipo de nómina no existe.");
            }

            foreach (var period in createPeriods.Dates.OrderBy(d => d.NumPeriod))
            {
                if (period.StartAdminDate >= period.ClosingAdminDate)
                {
                    throw new BadHttpRequestException($"La fecha de inicio no puede ser mayor o igual a la fecha fin, numero: {period.NumPeriod}.");
                }

                int days = (period.ClosingAdminDate.ToDateTime(TimeOnly.MinValue) - period.StartAdminDate.ToDateTime(TimeOnly.MinValue)).Days;

                if (days < payroll.Days - 1)
                {
                    // throw new BadHttpRequestException($"Los días entre fechas tiene que ser como mínimo {payroll.Days - 1}:{days}, numero: {period.NumPeriod}.");
                }

                var exist = _repository.GetByFilter((p) => p.NumPeriod == period.NumPeriod && p.TypePayroll == createPeriods.TypePayroll && p.Year == createPeriods.Year && p.Company == createPeriods.CompanyId).FirstOrDefault();

                if (exist == null) {
                    var datePayment = period.DatePayment != null ? (DateOnly)period.DatePayment : DateOnly.MinValue;
                    var newinsert = _repository.Create(new Models.Prenomina.Period()
                    {
                        TypePayroll = createPeriods.TypePayroll,
                        NumPeriod = period.NumPeriod,
                        Year = createPeriods.Year,
                        Company = createPeriods.CompanyId,
                        StartDate = period.StartDate,
                        ClosingDate = period.ClosingDate,
                        DatePayment = datePayment,
                        TotalDays = days,
                        StartAdminDate = period.StartAdminDate,
                        ClosingAdminDate = period.ClosingAdminDate,
                    });

                    result.Add(newinsert);
                } else
                {
                    exist.TotalDays = days;
                    exist.StartAdminDate = period.StartAdminDate;
                    exist.ClosingAdminDate = period.ClosingAdminDate;
                    exist.StartDate = period.StartDate;
                    exist.ClosingDate = period.ClosingDate;
                    if (period.DatePayment != null)
                    {
                        exist.DatePayment = (DateOnly)period.DatePayment;
                    }

                    var updated = _repository.Update(exist);

                    result.Add(updated);
                }
            }

            _repository.Save();
            var newId = result.Select(p => p.Id).ToList();

            var olds = _repository.GetByFilter((p) => p.TypePayroll == createPeriods.TypePayroll && p.Year == createPeriods.Year && p.Company == createPeriods.CompanyId && !newId.Contains(p.Id)).ToList();

            foreach (var old in olds)
            {
                _repository.Delete(old);
            }

            _repository.Save();

            return result;
        }

        public async Task<IEnumerable<Models.Prenomina.Period>> ExecuteProcess(CreatePeriodsByFile createPeriods)
        {
            if (createPeriods.File == null || createPeriods.File.Length == 0)
            {
                throw new BadHttpRequestException("Archivo no válido.");
            }

            var extension = Path.GetExtension(createPeriods.File.FileName).ToLower();

            if (extension != ".xlsx" && extension != ".csv")
            {
                throw new BadHttpRequestException("Formato de archivo no soportado, Formatos permitidos .xlsx o .csv.");
            }

            var listDates = new List<CreatePeriodLite>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await createPeriods.File.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".xlsx")
                    {
                        using (var workbook = new XLWorkbook(stream))
                        {
                            var worksheet = workbook.Worksheet(1);
                            foreach (var row in worksheet.RowsUsed().Skip(1))
                            {
                                var CNumPeriod = row.Cell(1);
                                var CStartDate = row.Cell(2);
                                var CClosingDate = row.Cell(3);
                                var CStartAdminDate = row.Cell(4);
                                var CClosingAdminDate = row.Cell(5);
                                var CDatePayment = row.Cell(6);
                                var CTotalDays = row.Cell(7);

                                listDates.Add(new CreatePeriodLite()
                                {
                                    NumPeriod = ((int)CNumPeriod.Value.GetNumber()),
                                    StartAdminDate = DateOnly.FromDateTime(CStartAdminDate.Value.GetDateTime()),
                                    ClosingAdminDate = DateOnly.FromDateTime(CClosingAdminDate.Value.GetDateTime()),
                                    StartDate = DateOnly.FromDateTime(CStartDate.Value.GetDateTime()),
                                    ClosingDate = DateOnly.FromDateTime(CClosingDate.Value.GetDateTime()),
                                    DatePayment = DateOnly.FromDateTime(CDatePayment.Value.GetDateTime()),
                                });
                            }
                        }
                    } else if (extension == ".csv")
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            await reader.ReadLineAsync();

                            while (!reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync();
                                if (line == null)
                                {
                                    continue;
                                }

                                var values = line.Split(',');
                                var CNumPeriod = int.Parse(values[0]);
                                var CStartDate = DateOnly.Parse(values[1]);
                                var CClosingDate = DateOnly.Parse(values[2]);
                                var CStartAdminDate = DateOnly.Parse(values[3]);
                                var CClosingAdminDate = DateOnly.Parse(values[4]);
                                var CDatePayment = DateOnly.Parse(values[5]);
                                var CTotalDays = int.Parse(values[6]);

                                listDates.Add(new CreatePeriodLite()
                                {
                                    NumPeriod = CNumPeriod,
                                    StartAdminDate = CStartAdminDate,
                                    ClosingAdminDate = CClosingAdminDate,
                                    StartDate = CStartDate,
                                    ClosingDate = CClosingDate,
                                    DatePayment = CDatePayment,
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw new BadHttpRequestException($"Error al procesar el archivo: {ex.Message}");
            }

            var result = ExecuteProcess<CreatePeriods, IEnumerable<Models.Prenomina.Period>>(new CreatePeriods()
            {
                Dates = listDates,
                TypePayroll = createPeriods.TypePayroll,
                Year = createPeriods.Year,
                CompanyId = createPeriods.CompanyId,
            });

            return result;
        }

        public IEnumerable<PeriodStatus> ExecuteProcess(ChangePeriodStatus changePeriodStatus)
        {
            if (changePeriodStatus.ByUserId == null)
            {
                throw new BadHttpRequestException("El usuario es requerido");
            }

            var findPeriod = _periodStatusRepo.GetByFilter(p => p.CompanyId == changePeriodStatus.CompanyId && p.Year == changePeriodStatus.Year && p.TypePayroll == changePeriodStatus.TypePayroll && p.TenantId == changePeriodStatus.TenantId && p.NumPeriod == changePeriodStatus.NumPeriod).FirstOrDefault();

            if (changePeriodStatus.TenantId == "-999")
            {
                var others = _periodStatusRepo.GetByFilter(p => p.CompanyId == changePeriodStatus.CompanyId && p.Year == changePeriodStatus.Year && p.TypePayroll == changePeriodStatus.TypePayroll && p.NumPeriod == changePeriodStatus.NumPeriod).ToList();
                foreach (var item in others)
                {
                    _periodStatusRepo.Delete(item);
                }
            }

            if (findPeriod == null) {
                _periodStatusRepo.Create(new PeriodStatus()
                {
                    CompanyId = changePeriodStatus.CompanyId,
                    NumPeriod = changePeriodStatus.NumPeriod,
                    TenantId = changePeriodStatus.TenantId,
                    Year = changePeriodStatus.Year,
                    TypePayroll = changePeriodStatus.TypePayroll,
                    ByUserId = Guid.Parse(changePeriodStatus.ByUserId),
                });
            } else
            {
                _periodStatusRepo.Delete(findPeriod);
            }

            _periodStatusRepo.Save();

            var newlist = _periodStatusRepo.GetAll();

            return newlist;
        }

        public bool ExecuteProcess(VerifyClosedPeriod verifyClosedPeriod)
        {
            var findPeriod = _periodStatusRepo.GetByFilter(p => p.CompanyId == verifyClosedPeriod.CompanyId && p.Year == verifyClosedPeriod.Year && p.TypePayroll == verifyClosedPeriod.TypePayroll && (p.TenantId == verifyClosedPeriod.TenantId || p.TenantId == "-999") && p.NumPeriod == verifyClosedPeriod.NumPeriod).FirstOrDefault();

            return findPeriod != null;
        }

        public Models.Prenomina.Period? ExecuteProcess(FindPeriod findPeriod)
        {
            var find = _repository.GetByFilter(p => findPeriod.Date >= p.StartDate && findPeriod.Date <= p.ClosingDate && p.TypePayroll == findPeriod.TypePayroll && p.Year == findPeriod.Year && p.Company == findPeriod.CompanyId).FirstOrDefault();

            return find;
        }
    }
}
