using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina.Helpers;
using System.Text.Json;
using Period = PrenominaApi.Models.Prenomina.Period;

namespace PrenominaApi.Services.Prenomina
{
    /// <summary>
    /// Genera el archivo de importación de tiempo extra YA AUTORIZADO de un periodo, con la
    /// estructura requerida por NóminaTISS-SAR:
    ///   CODIGO(8,N) | CONCEPTO(4,N) | IMPORTE(10.2,N) | FECHA(dd/mm/aaaa,C) | HORAS(6.2,N)
    ///
    /// Las horas se reparten entre conceptos 11/12/13 con
    /// <see cref="OvertimePaymentConceptCalculator"/> (un renglón por empleado, concepto y día).
    ///
    /// "Autorizadas" = días con papeleta de pago aprobada
    /// (<c>OvertimeDayDetail.PaymentRequestApproved == true</c>).
    /// </summary>
    public class OvertimePaymentFileService
    {
        // Factor del importe por concepto: IMPORTE = (sueldo / 8) * horas * factor.
        private const decimal DailyHours = 8m;
        private const decimal FactorDouble = 2m; // conceptos 11 y 12
        private const decimal FactorTriple = 3m; // concepto 13

        private readonly OvertimeAccumulationService _overtimeService;
        private readonly PrenominaDbContext _context;
        private readonly IBaseServicePrenomina<Period> _periodService;
        private readonly GlobalPropertyService _globalPropertyService;

        public OvertimePaymentFileService(
            OvertimeAccumulationService overtimeService,
            PrenominaDbContext context,
            IBaseServicePrenomina<Period> periodService,
            GlobalPropertyService globalPropertyService)
        {
            _overtimeService = overtimeService;
            _context = context;
            _periodService = periodService;
            _globalPropertyService = globalPropertyService;
        }

        /// <summary>
        /// Construye los renglones de importación (uno por empleado, concepto y día con horas &gt; 0).
        /// </summary>
        public async Task<List<OvertimePaymentConceptLine>> BuildLines(
            int typeNomina,
            int numPeriod,
            int companyId,
            string? tenant)
        {
            var year = _globalPropertyService.YearOfOperation;

            var period = _periodService.GetByFilter(
                    p => p.TypePayroll == typeNomina &&
                         p.Company == companyId &&
                         p.NumPeriod == numPeriod &&
                         p.Year == year)
                .FirstOrDefault()
                ?? throw new BadHttpRequestException("El periodo seleccionado no es válido.");

            var summaries = await _overtimeService.GetOvertimeSummary(typeNomina, numPeriod, companyId, tenant);

            // Sueldo diario por empleado.
            var employeeCodes = summaries.Select(s => (decimal)s.EmployeeCode).ToList();
            var salaries = await _context.Set<Employee>().AsNoTracking()
                .Where(e => e.Company == companyId && employeeCodes.Contains(e.Codigo))
                .ToDictionaryAsync(e => (int)e.Codigo, e => e.Salary);

            var lines = new List<OvertimePaymentConceptLine>();

            foreach (var emp in summaries)
            {
                // Solo días con papeleta de pago aprobada (horas extra autorizadas).
                var authorizedDays = emp.DayDetails
                    .Where(d => d.PaymentRequestApproved && d.OvertimeMinutes > 0)
                    .Select(d => new OvertimePaymentConceptCalculator.DayInput
                    {
                        Date = d.Date,
                        OvertimeMinutes = d.OvertimeMinutes,
                    });

                var segments = OvertimePaymentConceptCalculator.Calculate(authorizedDays, period.StartDate);
                salaries.TryGetValue(emp.EmployeeCode, out var dailySalary);

                foreach (var seg in segments)
                {
                    var hours = Math.Round(seg.Minutes / 60m, 2, MidpointRounding.AwayFromZero);
                    var factor = seg.Concept == OvertimePaymentConceptCalculator.Concept13
                        ? FactorTriple
                        : FactorDouble;
                    var amount = Math.Round((dailySalary / DailyHours) * hours * factor, 2, MidpointRounding.AwayFromZero);

                    lines.Add(new OvertimePaymentConceptLine
                    {
                        EmployeeCode = emp.EmployeeCode,
                        FullName = emp.FullName,
                        JobPosition = emp.JobPosition,
                        Concept = seg.Concept,
                        Amount = amount,
                        Date = seg.Date,
                        Hours = hours,
                    });
                }
            }

            return lines;
        }

        /// <summary>
        /// Estado de generación del archivo del periodo (indicador anti doble-pago).
        /// </summary>
        public async Task<OvertimePaymentFileStatus> GetGenerationStatus(int typeNomina, int numPeriod, int companyId)
        {
            var key = BuildStatusKey(companyId, typeNomina, numPeriod, _globalPropertyService.YearOfOperation);

            var row = await _context.systemConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key && c.DeletedAt == null);

            if (row == null)
            {
                return new OvertimePaymentFileStatus { Generated = false };
            }

            var info = JsonSerializer.Deserialize<GenerationInfo>(row.Data) ?? new GenerationInfo();

            string? byName = null;
            if (Guid.TryParse(info.GeneratedByUserId, out var uid))
            {
                byName = await _context.users.AsNoTracking()
                    .Where(u => u.Id == uid)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync();
            }

            return new OvertimePaymentFileStatus
            {
                Generated = true,
                GeneratedAt = info.FirstGeneratedAt,
                LastGeneratedAt = info.LastGeneratedAt,
                GenerationCount = info.GenerationCount,
                GeneratedByName = byName,
                LineCount = info.LineCount,
                TotalAmount = info.TotalAmount,
            };
        }

        /// <summary>
        /// Marca el periodo como generado (o suma una regeneración), guardando fecha, usuario y totales.
        /// </summary>
        public async Task RecordGeneration(
            int typeNomina,
            int numPeriod,
            int companyId,
            string? userId,
            List<OvertimePaymentConceptLine> lines)
        {
            var key = BuildStatusKey(companyId, typeNomina, numPeriod, _globalPropertyService.YearOfOperation);
            var now = DateTime.UtcNow;

            var row = await _context.systemConfigs
                .FirstOrDefaultAsync(c => c.Key == key && c.DeletedAt == null);

            var info = row != null
                ? (JsonSerializer.Deserialize<GenerationInfo>(row.Data) ?? new GenerationInfo())
                : new GenerationInfo { FirstGeneratedAt = now };

            info.LastGeneratedAt = now;
            info.GenerationCount += 1;
            info.GeneratedByUserId = userId;
            info.LineCount = lines.Count;
            info.TotalAmount = lines.Sum(l => l.Amount);

            var data = JsonSerializer.Serialize(info);

            if (row == null)
            {
                _context.systemConfigs.Add(new SystemConfig { Key = key, Data = data });
            }
            else
            {
                row.Data = data;
                row.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        private static string BuildStatusKey(int companyId, int typeNomina, int numPeriod, int year)
            => $"overtime_extra_generated:{companyId}:{typeNomina}:{numPeriod}:{year}";

        /// <summary>Estructura serializada en SystemConfig.Data.</summary>
        private sealed class GenerationInfo
        {
            public DateTime FirstGeneratedAt { get; set; }
            public DateTime LastGeneratedAt { get; set; }
            public int GenerationCount { get; set; }
            public string? GeneratedByUserId { get; set; }
            public int LineCount { get; set; }
            public decimal TotalAmount { get; set; }
        }

        /// <summary>
        /// Genera el archivo XLSX con la estructura de importación.
        /// </summary>
        public (string fileName, byte[] content) GenerateFile(
            List<OvertimePaymentConceptLine> lines,
            int numPeriod)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("TpoExtra");

            // Encabezado.
            ws.Cell("A1").Value = "CODIGO";
            ws.Cell("B1").Value = "CONCEPTO";
            ws.Cell("C1").Value = "IMPORTE";
            ws.Cell("D1").Value = "FECHA";
            ws.Cell("E1").Value = "HORAS";

            var row = 2;
            foreach (var line in lines)
            {
                ws.Cell($"A{row}").Value = line.EmployeeCode;
                ws.Cell($"B{row}").Value = line.Concept;

                var importe = ws.Cell($"C{row}");
                importe.Value = line.Amount;
                importe.Style.NumberFormat.Format = "0.00";

                // FECHA es campo de texto (Tipo C) con formato dd/mm/aaaa.
                ws.Cell($"D{row}").Value = line.Date.ToString("dd/MM/yyyy");

                var horas = ws.Cell($"E{row}");
                horas.Value = line.Hours;
                horas.Style.NumberFormat.Format = "0.00";

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return ($"tiempo-extra-periodo-{numPeriod}.xlsx", stream.ToArray());
        }
    }
}
