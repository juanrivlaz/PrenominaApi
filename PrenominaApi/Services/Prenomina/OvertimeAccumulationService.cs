using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Services.Prenomina.Helpers;
using System.Text.Json;
using Period = PrenominaApi.Models.Prenomina.Period;

namespace PrenominaApi.Services.Prenomina
{
    public class OvertimeAccumulationService
    {
        private readonly PrenominaDbContext _context;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly IBaseServicePrenomina<Period> _periodService;
        private readonly ApprovalResolver _approvalResolver;

        public OvertimeAccumulationService(
            PrenominaDbContext context,
            IBaseRepository<Key> keyRepository,
            GlobalPropertyService globalPropertyService,
            IBaseServicePrenomina<Period> periodService,
            ApprovalResolver approvalResolver)
        {
            _context = context;
            _keyRepository = keyRepository;
            _globalPropertyService = globalPropertyService;
            _periodService = periodService;
            _approvalResolver = approvalResolver;
        }

        /// <summary>
        /// Crea la papeleta/solicitud de pago de horas extras para un empleado, vincula los
        /// movimientos de pago indicados y materializa su cadena de firmas (documento módulo
        /// Pago de horas extras). Una papeleta por empleado.
        /// </summary>
        /// <summary>
        /// Verifica que exista un documento/formato de módulo "Pago de horas extras". Sin él no
        /// se puede generar la papeleta, por lo que no se permite pagar.
        /// </summary>
        private async Task EnsureOvertimePaymentDocumentExists()
        {
            var exists = await _context.documents
                .AnyAsync(d => d.Module == DocumentModule.OvertimePayment && d.DeletedAt == null);

            if (!exists)
            {
                throw new BadHttpRequestException(
                    "No existe un formato (papeleta) de horas extras configurado. Crea uno en Documentos (módulo Pago de horas extras) antes de pagar.");
            }
        }

        private async Task CreateOvertimePaymentRequest(int employeeCode, int companyId, string? userId, List<int> movementIds, int totalMinutes, string? notes)
        {
            if (movementIds.Count == 0)
            {
                return;
            }

            var documentId = await _context.documents
                .Where(d => d.Module == DocumentModule.OvertimePayment && d.DeletedAt == null)
                .OrderBy(d => d.CreatedAt)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync();

            var request = new OvertimePaymentRequest
            {
                EmployeeCode = employeeCode,
                CompanyId = companyId,
                TotalMinutes = totalMinutes,
                DocumentId = documentId,
                Status = AbsenceRequestStatus.Pending,
                Notes = notes,
                CreatedByUserId = Guid.TryParse(userId, out var uid) ? uid : (Guid?)null,
            };
            _context.overtimePaymentRequests.Add(request);

            var movements = await _context.overtimeMovementLogs
                .Where(m => movementIds.Contains(m.Id))
                .ToListAsync();
            foreach (var m in movements)
            {
                m.OvertimePaymentRequestId = request.Id;
            }

            await _context.SaveChangesAsync();

            // Materializa la cadena de firmas del documento (si está configurado).
            _approvalResolver.MaterializeForRequest(ApprovalRequestType.OvertimePayment, request.Id, documentId, companyId, employeeCode);
        }

        /// <summary>
        /// Obtiene el balance de acumulación de un empleado
        /// </summary>
        public async Task<OvertimeAccumulationOutput?> GetEmployeeAccumulation(int employeeCode, int companyId)
        {
            var accumulation = await _context.overtimeAccumulations
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeCode == employeeCode && a.CompanyId == companyId);

            if (accumulation == null)
            {
                return null;
            }

            var employee = await GetEmployeeInfo(employeeCode, companyId);

            return new OvertimeAccumulationOutput
            {
                EmployeeCode = employeeCode,
                FullName = employee?.FullName ?? string.Empty,
                Department = employee?.Department ?? string.Empty,
                JobPosition = employee?.JobPosition ?? string.Empty,
                AvailableMinutes = accumulation.AccumulatedMinutes,
                AvailableFormatted = FormatMinutes(accumulation.AccumulatedMinutes),
                TotalAccumulatedMinutes = accumulation.AccumulatedMinutes + accumulation.UsedMinutes + accumulation.PaidMinutes,
                TotalUsedMinutes = accumulation.UsedMinutes,
                TotalPaidMinutes = accumulation.PaidMinutes,
                LastUpdated = accumulation.UpdatedAt
            };
        }

        /// <summary>
        /// Obtiene el resumen de horas extras con opciones de acumulación
        /// </summary>
        public async Task<List<OvertimeSummaryOutput>> GetOvertimeSummary(
            int typeNomina,
            int numPeriod,
            int companyId,
            string? tenant,
            string? search = null)
        {
            var year = _globalPropertyService.YearOfOperation;

            var period = _periodService.GetByFilter(
                p => p.TypePayroll == typeNomina &&
                     p.Company == companyId &&
                     p.NumPeriod == numPeriod &&
                     p.Year == year)
                .FirstOrDefault();

            if (period == null)
            {
                throw new BadHttpRequestException("El periodo seleccionado no es válido.");
            }

            var lowerSearch = search?.ToLower();

            // Obtener empleados
            var employees = await _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k =>
                    k.Company == companyId &&
                    k.TypeNom == typeNomina &&
                    (
                        tenant == "-999" ||
                        (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                            k.Center == tenant :
                            k.Supervisor == Convert.ToDecimal(tenant))
                    ) &&
                    (
                        string.IsNullOrEmpty(lowerSearch) ||
                        k.Codigo.ToString().Contains(lowerSearch) ||
                        (k.Employee.Name + " " + k.Employee.LastName + " " + k.Employee.MLastName).ToLower().Contains(lowerSearch)
                    ))
                .Select(k => new
                {
                    k.Codigo,
                    k.Ocupation,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                })
                .ToListAsync();

            if (!employees.Any())
            {
                return new List<OvertimeSummaryOutput>();
            }

            // Filtrar empleados excluidos de horas extras por actividad
            var activityIds = employees.Select(e => e.Ocupation).Distinct().ToList();
            var excludedActivities = await _context.activityOvertimeConfigs
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && activityIds.Contains(c.ActivityId) && c.ExcludeOvertime)
                .Select(c => c.ActivityId)
                .ToListAsync();
            var excludedActivitySet = new HashSet<int>(excludedActivities);

            // Filtrar empleados excluidos individualmente (prioridad sobre actividad)
            var allCodes = employees.Select(e => (int)e.Codigo).ToList();
            var employeeConfigs = await _context.employeeOvertimeConfigs
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && allCodes.Contains(c.EmployeeCode))
                .ToDictionaryAsync(c => c.EmployeeCode, c => c.ExcludeOvertime);

            employees = employees.Where(e =>
            {
                var code = (int)e.Codigo;
                // Si existe config individual, tiene prioridad
                if (employeeConfigs.TryGetValue(code, out var excludeByEmployee))
                    return !excludeByEmployee;
                // Si no, verificar por actividad
                if (excludedActivitySet.Contains(e.Ocupation))
                    return false;
                return true;
            }).ToList();

            if (!employees.Any())
            {
                return new List<OvertimeSummaryOutput>();
            }

            var employeeCodes = employees.Select(e => e.Codigo).ToList();
            var employeeCodesJson = JsonSerializer.Serialize(employeeCodes);

            // Obtener check-ins con tiempo extra
            var overtimeData = await _context.Database.SqlQueryRaw<OvertimeQueryResult>(
                """
                WITH WorkSummary AS (
                    SELECT
                        eci.employee_code AS EmployeeCode,
                        eci.[date],
                        MIN(eci.check_in) AS CheckIn,
                        MAX(CASE WHEN eci.EoS = 1 THEN eci.check_in END) AS CheckOut
                    FROM employee_check_ins eci
                    WHERE
                        eci.[date] BETWEEN @startDate AND @closingDate
                        AND eci.employee_code IN (
                            SELECT value FROM OPENJSON(@codes)
                        )
                    GROUP BY
                        eci.employee_code,
                        eci.[date]
                )
                SELECT *,
                    DATEDIFF(MINUTE, CheckIn, CheckOut) AS TotalMinutesWorked
                FROM WorkSummary
                WHERE
                    CheckOut IS NOT NULL
                    AND DATEDIFF(MINUTE, CheckIn, CheckOut) >= (60 * 8) + 30
                ORDER BY
                    EmployeeCode, [date];
                """,
                new SqlParameter("@codes", employeeCodesJson),
                new SqlParameter("@startDate", period.StartDate),
                new SqlParameter("@closingDate", period.ClosingDate)
            ).ToListAsync();

            // Obtener registros externos en el período
            var externalEntries = await _context.overtimeMovementLogs
                .AsNoTracking()
                .Where(m =>
                    m.CompanyId == companyId &&
                    employeeCodes.Contains(m.EmployeeCode) &&
                    m.SourceDate >= period.StartDate &&
                    m.SourceDate <= period.ClosingDate &&
                    m.MovementType == OvertimeMovementType.ExternalEntry)
                .ToListAsync();

            // Obtener movimientos de procesamiento existentes en el período
            var existingMovements = await _context.overtimeMovementLogs
                .AsNoTracking()
                .Where(m =>
                    m.CompanyId == companyId &&
                    employeeCodes.Contains(m.EmployeeCode) &&
                    m.SourceDate >= period.StartDate &&
                    m.SourceDate <= period.ClosingDate &&
                    (m.MovementType == OvertimeMovementType.Accumulation ||
                     m.MovementType == OvertimeMovementType.DirectPayment ||
                     m.MovementType == OvertimeMovementType.HourBank))
                .ToListAsync();

            // Obtener IDs de movimientos cancelados (para filtrarlos)
            var allMovementIds = existingMovements.Select(m => m.Id)
                .Concat(externalEntries.Select(m => m.Id)).ToList();
            var existingMovementIds = allMovementIds;
            var cancelledMovementIds = await _context.overtimeMovementLogs
                .AsNoTracking()
                .Where(m =>
                    m.CompanyId == companyId &&
                    m.MovementType == OvertimeMovementType.Cancellation &&
                    m.RelatedMovementId != null &&
                    existingMovementIds.Contains(m.RelatedMovementId.Value))
                .Select(m => m.RelatedMovementId!.Value)
                .ToListAsync();

            var cancelledSet = new HashSet<int>(cancelledMovementIds);

            // Estado de las papeletas de pago vinculadas a los pagos directos (para saber si un
            // día pagado todavía se puede cancelar: solo si su papeleta NO está aprobada).
            var paymentRequestIds = existingMovements
                .Where(m => m.MovementType == OvertimeMovementType.DirectPayment && m.OvertimePaymentRequestId != null)
                .Select(m => m.OvertimePaymentRequestId!.Value)
                .Distinct()
                .ToList();
            var paymentRequestStatusById = paymentRequestIds.Count == 0
                ? new Dictionary<Guid, AbsenceRequestStatus>()
                : await _context.overtimePaymentRequests.AsNoTracking()
                    .Where(r => paymentRequestIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Status);

            // Obtener balances actuales
            var balances = await _context.overtimeAccumulations
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId && employeeCodes.Contains(a.EmployeeCode))
                .ToDictionaryAsync(a => a.EmployeeCode, a => a.AccumulatedMinutes);

            var movementsByEmployeeDate = existingMovements
                .GroupBy(m => (m.EmployeeCode, m.SourceDate))
                .ToDictionary(g => g.Key, g => g.Where(m => !cancelledSet.Contains(m.Id)).ToList());

            var result = new List<OvertimeSummaryOutput>();

            // Agrupar registros externos por empleado
            var externalByEmployee = externalEntries
                .GroupBy(e => e.EmployeeCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Obtener las fechas de check-ins procesadas para evitar duplicados con externos
            var checkinDatesByEmployee = overtimeData
                .GroupBy(o => o.EmployeeCode)
                .ToDictionary(g => g.Key, g => g.Select(o => o.Date).ToHashSet());

            foreach (var emp in employees)
            {
                var empOvertimes = overtimeData.Where(o => o.EmployeeCode == emp.Codigo).ToList();
                var empExternals = externalByEmployee.GetValueOrDefault((int)emp.Codigo, new List<OvertimeMovementLog>());

                // Filtrar externos cancelados y los que ya tienen un día de check-in en la misma fecha
                checkinDatesByEmployee.TryGetValue((int)emp.Codigo, out var checkinDates);
                var pendingExternals = empExternals
                    .Where(e => !cancelledSet.Contains(e.Id))
                    .Where(e => checkinDates == null || !checkinDates.Contains(e.SourceDate))
                    .ToList();

                if (!empOvertimes.Any() && !pendingExternals.Any())
                {
                    continue;
                }

                var dayDetails = new List<OvertimeDayDetail>();
                int accumulatedInPeriod = 0;
                int paidInPeriod = 0;
                int pendingInPeriod = 0;

                // Procesar días de check-ins
                foreach (var day in empOvertimes)
                {
                    var overtimeMinutes = day.TotalMinutesWorked - (8 * 60);

                    if (movementsByEmployeeDate.TryGetValue((day.EmployeeCode, day.Date), out var movements) && movements.Any())
                    {
                        var processedMinutes = movements.Sum(m => m.Minutes);
                        var remaining = overtimeMinutes - processedMinutes;

                        foreach (var mov in movements)
                        {
                            var movStatus = mov.MovementType switch
                            {
                                OvertimeMovementType.Accumulation => OvertimeDayStatus.Accumulated,
                                OvertimeMovementType.DirectPayment => OvertimeDayStatus.Paid,
                                OvertimeMovementType.HourBank => OvertimeDayStatus.HourBank,
                                _ => OvertimeDayStatus.Pending
                            };

                            if (movStatus == OvertimeDayStatus.Accumulated)
                                accumulatedInPeriod += mov.Minutes;
                            else if (movStatus == OvertimeDayStatus.Paid)
                                paidInPeriod += mov.Minutes;
                        }

                        // Si queda tiempo sin procesar, agregar fila pendiente
                        if (remaining > 0)
                        {
                            pendingInPeriod += remaining;
                            dayDetails.Add(new OvertimeDayDetail
                            {
                                Date = day.Date,
                                CheckIn = day.CheckIn,
                                CheckOut = day.CheckOut,
                                TotalMinutesWorked = day.TotalMinutesWorked,
                                OvertimeMinutes = remaining,
                                OvertimeFormatted = FormatMinutes(remaining),
                                Status = OvertimeDayStatus.Pending,
                                StatusLabel = "Pendiente",
                                MovementId = null
                            });
                        }

                        // Agregar filas de cada movimiento procesado
                        foreach (var mov in movements)
                        {
                            var movStatus = mov.MovementType switch
                            {
                                OvertimeMovementType.Accumulation => OvertimeDayStatus.Accumulated,
                                OvertimeMovementType.DirectPayment => OvertimeDayStatus.Paid,
                                OvertimeMovementType.HourBank => OvertimeDayStatus.HourBank,
                                _ => OvertimeDayStatus.Pending
                            };

                            dayDetails.Add(new OvertimeDayDetail
                            {
                                Date = day.Date,
                                CheckIn = day.CheckIn,
                                CheckOut = day.CheckOut,
                                TotalMinutesWorked = day.TotalMinutesWorked,
                                OvertimeMinutes = mov.Minutes,
                                OvertimeFormatted = FormatMinutes(mov.Minutes),
                                Status = movStatus,
                                StatusLabel = GetStatusLabel(movStatus),
                                MovementId = mov.Id,
                                PaymentRequestId = mov.OvertimePaymentRequestId,
                                PaymentRequestApproved = mov.OvertimePaymentRequestId != null
                                    && paymentRequestStatusById.TryGetValue(mov.OvertimePaymentRequestId.Value, out var prStatusA) && prStatusA == AbsenceRequestStatus.Approved
                            });
                        }
                    }
                    else
                    {
                        pendingInPeriod += overtimeMinutes;
                        dayDetails.Add(new OvertimeDayDetail
                        {
                            Date = day.Date,
                            CheckIn = day.CheckIn,
                            CheckOut = day.CheckOut,
                            TotalMinutesWorked = day.TotalMinutesWorked,
                            OvertimeMinutes = overtimeMinutes,
                            OvertimeFormatted = FormatMinutes(overtimeMinutes),
                            Status = OvertimeDayStatus.Pending,
                            StatusLabel = GetStatusLabel(OvertimeDayStatus.Pending),
                            MovementId = null
                        });
                    }
                }

                // Agregar registros externos como días pendientes
                foreach (var ext in pendingExternals)
                {
                    // Verificar si ya fue procesado (acumulado/pagado/banco)
                    if (movementsByEmployeeDate.TryGetValue((ext.EmployeeCode, ext.SourceDate), out var procMovements) && procMovements.Any())
                    {
                        var processedMin = procMovements.Sum(m => m.Minutes);
                        var remainingExt = ext.Minutes - processedMin;

                        foreach (var mov in procMovements)
                        {
                            var movStatus = mov.MovementType switch
                            {
                                OvertimeMovementType.Accumulation => OvertimeDayStatus.Accumulated,
                                OvertimeMovementType.DirectPayment => OvertimeDayStatus.Paid,
                                OvertimeMovementType.HourBank => OvertimeDayStatus.HourBank,
                                _ => OvertimeDayStatus.Pending
                            };

                            if (movStatus == OvertimeDayStatus.Accumulated)
                                accumulatedInPeriod += mov.Minutes;
                            else if (movStatus == OvertimeDayStatus.Paid)
                                paidInPeriod += mov.Minutes;

                            dayDetails.Add(new OvertimeDayDetail
                            {
                                Date = ext.SourceDate,
                                CheckIn = TimeOnly.MinValue,
                                CheckOut = null,
                                TotalMinutesWorked = ext.Minutes,
                                OvertimeMinutes = mov.Minutes,
                                OvertimeFormatted = $"{FormatMinutes(mov.Minutes)} (externo)",
                                Status = movStatus,
                                StatusLabel = GetStatusLabel(movStatus),
                                MovementId = mov.Id,
                                PaymentRequestId = mov.OvertimePaymentRequestId,
                                PaymentRequestApproved = mov.OvertimePaymentRequestId != null
                                    && paymentRequestStatusById.TryGetValue(mov.OvertimePaymentRequestId.Value, out var prStatusB) && prStatusB == AbsenceRequestStatus.Approved
                            });
                        }

                        if (remainingExt > 0)
                        {
                            pendingInPeriod += remainingExt;
                            dayDetails.Add(new OvertimeDayDetail
                            {
                                Date = ext.SourceDate,
                                CheckIn = TimeOnly.MinValue,
                                CheckOut = null,
                                TotalMinutesWorked = ext.Minutes,
                                OvertimeMinutes = remainingExt,
                                OvertimeFormatted = $"{FormatMinutes(remainingExt)} (externo)",
                                Status = OvertimeDayStatus.Pending,
                                StatusLabel = "Pendiente (externo)",
                                MovementId = null
                            });
                        }

                        continue;
                    }

                    pendingInPeriod += ext.Minutes;

                    dayDetails.Add(new OvertimeDayDetail
                    {
                        Date = ext.SourceDate,
                        CheckIn = TimeOnly.MinValue,
                        CheckOut = null,
                        TotalMinutesWorked = ext.Minutes,
                        OvertimeMinutes = ext.Minutes,
                        OvertimeFormatted = $"{FormatMinutes(ext.Minutes)} (externo)",
                        Status = OvertimeDayStatus.Pending,
                        StatusLabel = "Pendiente (externo)",
                        MovementId = null
                    });
                }

                // Ordenar dayDetails por fecha
                dayDetails = dayDetails.OrderBy(d => d.Date).ToList();

                balances.TryGetValue((int)emp.Codigo, out var currentBalance);

                result.Add(new OvertimeSummaryOutput
                {
                    EmployeeCode = (int)emp.Codigo,
                    FullName = emp.FullName,
                    Department = emp.Department ?? string.Empty,
                    JobPosition = emp.JobPosition ?? string.Empty,
                    TotalOvertimeMinutes = dayDetails.Sum(d => d.OvertimeMinutes),
                    TotalOvertimeFormatted = FormatMinutes(dayDetails.Sum(d => d.OvertimeMinutes)),
                    AccumulatedMinutes = accumulatedInPeriod,
                    PaidMinutes = paidInPeriod,
                    PaidMinutesFormatted = FormatMinutes(paidInPeriod),
                    PendingMinutes = pendingInPeriod,
                    CurrentBalance = currentBalance,
                    CurrentBalanceFormatted = FormatMinutes(currentBalance),
                    DayDetails = dayDetails
                });
            }

            return result;
        }

        /// <summary>
        /// Acumula horas extras para un empleado
        /// </summary>
        public async Task<OvertimeOperationResult> AccumulateOvertime(AccumulateOvertimeInput input, int companyId, string? userId)
        {
            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);

            accumulation.AccumulatedMinutes += input.Minutes;
            accumulation.UpdatedAt = DateTime.UtcNow;

            // Crear log del movimiento
            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.Accumulation,
                Minutes = input.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.SourceDate,
                OriginalCheckIn = input.CheckIn,
                OriginalCheckOut = input.CheckOut,
                Notes = input.Notes,
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Horas acumuladas correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Registra pago directo de horas extras (sin acumular)
        /// </summary>
        public async Task<OvertimeOperationResult> PayOvertimeDirect(PayOvertimeDirectInput input, int companyId, string? userId, bool createPaymentRequest = true)
        {
            // Sin formato de horas extras no se puede generar la papeleta -> no se permite pagar.
            // (En lote la validación se hace una vez en ProcessOvertimesBatch.)
            if (createPaymentRequest)
            {
                await EnsureOvertimePaymentDocumentExists();
            }

            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);
            accumulation.PaidMinutes += input.Minutes;
            accumulation.UpdatedAt = DateTime.UtcNow;

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.DirectPayment,
                Minutes = input.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.SourceDate,
                OriginalCheckIn = input.CheckIn,
                OriginalCheckOut = input.CheckOut,
                Notes = input.Notes ?? "Pago directo de tiempo extra",
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            // Pago individual: genera su papeleta de inmediato. En lote se omite aquí y se crea
            // una sola papeleta por empleado (ver ProcessOvertimesBatch).
            if (createPaymentRequest)
            {
                await CreateOvertimePaymentRequest(input.EmployeeCode, companyId, userId,
                    new List<int> { movementLog.Id }, input.Minutes, input.Notes);
            }

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Pago registrado correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Usa horas acumuladas para día de descanso
        /// </summary>
        public async Task<OvertimeOperationResult> UseForRestDay(UseOvertimeForRestDayInput input, int companyId, string? userId)
        {
            var accumulation = await _context.overtimeAccumulations
                .FirstOrDefaultAsync(a => a.EmployeeCode == input.EmployeeCode && a.CompanyId == companyId);

            if (accumulation == null || accumulation.AccumulatedMinutes < input.MinutesToUse)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "No hay suficientes horas acumuladas disponibles."
                };
            }

            // Obtener las fechas origen de los movimientos usados
            string sourceNotes = input.Notes ?? "";
            if (input.SourceMovementIds != null && input.SourceMovementIds.Any())
            {
                var sourceMovements = await _context.overtimeMovementLogs
                    .Where(m => input.SourceMovementIds.Contains(m.Id))
                    .Select(m => m.SourceDate.ToString("dd/MM/yyyy"))
                    .ToListAsync();

                sourceNotes = $"Origen: {string.Join(", ", sourceMovements)}. {sourceNotes}";
            }

            accumulation.AccumulatedMinutes -= input.MinutesToUse;
            accumulation.UsedMinutes += input.MinutesToUse;
            accumulation.UpdatedAt = DateTime.UtcNow;

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.UsedForRestDay,
                Minutes = -input.MinutesToUse, // Negativo porque se descuenta
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.RestDate, // La fecha del descanso como referencia
                AppliedRestDate = input.RestDate,
                Notes = sourceNotes,
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Día de descanso aplicado correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Obtiene los minutos acumulados disponibles de un empleado (0 si no tiene registro).
        /// </summary>
        public async Task<int> GetAvailableMinutes(int employeeCode, int companyId)
        {
            var accumulation = await _context.overtimeAccumulations
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeCode == employeeCode && a.CompanyId == companyId);

            return accumulation?.AccumulatedMinutes ?? 0;
        }

        /// <summary>
        /// Usa horas acumuladas para cubrir un día de permiso/ausencia, relacionando el
        /// consumo con la incidencia para poder reintegrarlo si el permiso se cancela.
        /// </summary>
        public async Task<OvertimeOperationResult> UseForTimeOff(UseOvertimeForTimeOffInput input, int companyId, string? userId)
        {
            if (input.MinutesToUse <= 0)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Los minutos a utilizar deben ser mayores a cero."
                };
            }

            var accumulation = await _context.overtimeAccumulations
                .FirstOrDefaultAsync(a => a.EmployeeCode == input.EmployeeCode && a.CompanyId == companyId);

            if (accumulation == null || accumulation.AccumulatedMinutes < input.MinutesToUse)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "No hay suficientes horas acumuladas disponibles."
                };
            }

            accumulation.AccumulatedMinutes -= input.MinutesToUse;
            accumulation.UsedMinutes += input.MinutesToUse;
            accumulation.UpdatedAt = DateTime.UtcNow;

            var notes = $"Permiso del {input.TimeOffDate:dd/MM/yyyy}.";
            if (!string.IsNullOrWhiteSpace(input.Notes))
            {
                notes = $"{notes} {input.Notes}";
            }

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.UsedForTimeOff,
                Minutes = -input.MinutesToUse, // Negativo porque se descuenta
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.TimeOffDate,
                AppliedRestDate = input.TimeOffDate,
                AppliedIncidentId = input.AppliedIncidentId,
                Notes = notes,
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Horas aplicadas al permiso correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Reintegra al balance las horas que se habían consumido para los permisos indicados
        /// (por incidencia). Genera un movimiento de cancelación por cada consumo activo.
        /// Devuelve el total de minutos reintegrados.
        /// </summary>
        public async Task<int> RefundTimeOffForIncidents(IEnumerable<Guid> incidentIds, int companyId, string? userId, string reason)
        {
            var ids = incidentIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return 0;
            }

            // Consumos de horas para esos permisos
            var usageMovements = await _context.overtimeMovementLogs
                .Where(m => m.CompanyId == companyId &&
                            m.MovementType == OvertimeMovementType.UsedForTimeOff &&
                            m.AppliedIncidentId != null &&
                            ids.Contains(m.AppliedIncidentId.Value))
                .ToListAsync();

            if (usageMovements.Count == 0)
            {
                return 0;
            }

            // Movimientos ya cancelados previamente
            var usageIds = usageMovements.Select(m => m.Id).ToList();
            var cancelledIds = await _context.overtimeMovementLogs
                .Where(m => m.MovementType == OvertimeMovementType.Cancellation &&
                            m.RelatedMovementId != null &&
                            usageIds.Contains(m.RelatedMovementId.Value))
                .Select(m => m.RelatedMovementId!.Value)
                .ToListAsync();

            var byUser = Guid.Parse(userId ?? Guid.Empty.ToString());
            var totalRefunded = 0;

            foreach (var movement in usageMovements)
            {
                if (cancelledIds.Contains(movement.Id))
                {
                    continue; // Ya fue reintegrado
                }

                var accumulation = await _context.overtimeAccumulations
                    .FirstOrDefaultAsync(a => a.Id == movement.OvertimeAccumulationId);

                if (accumulation == null)
                {
                    continue;
                }

                // movement.Minutes es negativo (se descontó). Revertir:
                accumulation.AccumulatedMinutes -= movement.Minutes; // suma de vuelta
                accumulation.UsedMinutes += movement.Minutes;        // resta de used
                accumulation.UpdatedAt = DateTime.UtcNow;

                _context.overtimeMovementLogs.Add(new OvertimeMovementLog
                {
                    OvertimeAccumulationId = accumulation.Id,
                    EmployeeCode = movement.EmployeeCode,
                    CompanyId = companyId,
                    MovementType = OvertimeMovementType.Cancellation,
                    Minutes = -movement.Minutes,
                    BalanceAfter = accumulation.AccumulatedMinutes,
                    SourceDate = movement.SourceDate,
                    AppliedIncidentId = movement.AppliedIncidentId,
                    Notes = $"Reintegro: {reason}",
                    ByUserId = byUser,
                    RelatedMovementId = movement.Id,
                    CreatedAt = DateTime.UtcNow
                });

                totalRefunded += -movement.Minutes;
            }

            if (totalRefunded > 0)
            {
                await _context.SaveChangesAsync();
            }

            return totalRefunded;
        }

        /// <summary>
        /// Ajuste manual de horas
        /// </summary>
        public async Task<OvertimeOperationResult> ManualAdjustment(ManualOvertimeAdjustmentInput input, int companyId, string? userId)
        {
            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);

            if (input.Minutes < 0 && Math.Abs(input.Minutes) > accumulation.AccumulatedMinutes)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "El ajuste negativo excede el balance disponible."
                };
            }

            accumulation.AccumulatedMinutes += input.Minutes;
            accumulation.UpdatedAt = DateTime.UtcNow;

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.ManualAdjustment,
                Minutes = input.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.ReferenceDate,
                Notes = input.Notes,
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Ajuste realizado correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Cancela un movimiento previo
        /// </summary>
        public async Task<OvertimeOperationResult> CancelMovement(CancelOvertimeMovementInput input, int companyId, string? userId)
        {
            var movement = await _context.overtimeMovementLogs
                .Include(m => m.OvertimeAccumulation)
                .FirstOrDefaultAsync(m => m.Id == input.MovementId && m.CompanyId == companyId);

            if (movement == null)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Movimiento no encontrado."
                };
            }

            // Verificar que no esté ya cancelado
            var alreadyCancelled = await _context.overtimeMovementLogs
                .AnyAsync(m => m.RelatedMovementId == input.MovementId && m.MovementType == OvertimeMovementType.Cancellation);

            if (alreadyCancelled)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Este movimiento ya fue cancelado."
                };
            }

            // Si el movimiento es un pago directo vinculado a una papeleta de pago, la cancelación
            // es en cascada: no se puede cancelar si la papeleta ya fue aprobada; si sigue pendiente,
            // se elimina la papeleta y se revierten TODOS los pagos relacionados (vuelven a pendientes).
            if (movement.MovementType == OvertimeMovementType.DirectPayment && movement.OvertimePaymentRequestId != null)
            {
                var paymentRequest = await _context.overtimePaymentRequests
                    .FirstOrDefaultAsync(r => r.Id == movement.OvertimePaymentRequestId.Value && r.DeletedAt == null);

                if (paymentRequest != null)
                {
                    if (paymentRequest.Status == AbsenceRequestStatus.Approved)
                    {
                        return new OvertimeOperationResult
                        {
                            Success = false,
                            Message = "No se puede cancelar: la solicitud de pago ya fue aprobada."
                        };
                    }

                    return await CancelPaymentRequestCascade(paymentRequest, input.Reason, companyId, userId);
                }
            }

            var accumulation = movement.OvertimeAccumulation!;

            // Revertir el efecto según el tipo de movimiento
            switch (movement.MovementType)
            {
                case OvertimeMovementType.Accumulation:
                    accumulation.AccumulatedMinutes -= movement.Minutes;
                    break;
                case OvertimeMovementType.DirectPayment:
                    accumulation.PaidMinutes -= movement.Minutes;
                    break;
                case OvertimeMovementType.UsedForRestDay:
                case OvertimeMovementType.UsedForTimeOff:
                    accumulation.AccumulatedMinutes -= movement.Minutes; // Era negativo, así que se suma
                    accumulation.UsedMinutes += movement.Minutes; // Era positivo en used
                    break;
                case OvertimeMovementType.ManualAdjustment:
                    accumulation.AccumulatedMinutes -= movement.Minutes;
                    break;
                case OvertimeMovementType.HourBank:
                    accumulation.AccumulatedMinutes -= movement.Minutes;
                    break;
            }

            accumulation.UpdatedAt = DateTime.UtcNow;

            var cancellationLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = movement.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.Cancellation,
                Minutes = -movement.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = movement.SourceDate,
                Notes = $"Cancelación: {input.Reason}",
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                RelatedMovementId = movement.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(cancellationLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Movimiento cancelado correctamente.",
                MovementId = cancellationLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Cancela una papeleta de pago pendiente y revierte TODOS sus pagos directos: cada
        /// movimiento de pago vinculado se cancela (se reintegran los minutos pagados, los días
        /// vuelven a "pendientes"), se elimina (soft-delete) la papeleta y su cadena de firmas.
        /// </summary>
        private async Task<OvertimeOperationResult> CancelPaymentRequestCascade(
            OvertimePaymentRequest request, string? reason, int companyId, string? userId)
        {
            var now = DateTime.UtcNow;
            var byUser = Guid.Parse(userId ?? Guid.Empty.ToString());

            var movements = await _context.overtimeMovementLogs
                .Include(m => m.OvertimeAccumulation)
                .Where(m => m.OvertimePaymentRequestId == request.Id
                    && m.MovementType == OvertimeMovementType.DirectPayment)
                .ToListAsync();

            var movementIds = movements.Select(m => m.Id).ToList();
            var alreadyCancelled = (await _context.overtimeMovementLogs
                .Where(m => m.MovementType == OvertimeMovementType.Cancellation
                    && m.RelatedMovementId != null
                    && movementIds.Contains(m.RelatedMovementId.Value))
                .Select(m => m.RelatedMovementId!.Value)
                .ToListAsync())
                .ToHashSet();

            var cancelledCount = 0;
            foreach (var movement in movements)
            {
                if (alreadyCancelled.Contains(movement.Id))
                {
                    continue;
                }

                var accumulation = movement.OvertimeAccumulation;
                if (accumulation == null)
                {
                    continue;
                }

                accumulation.PaidMinutes -= movement.Minutes; // revertir lo pagado
                accumulation.UpdatedAt = now;

                _context.overtimeMovementLogs.Add(new OvertimeMovementLog
                {
                    OvertimeAccumulationId = accumulation.Id,
                    EmployeeCode = movement.EmployeeCode,
                    CompanyId = companyId,
                    MovementType = OvertimeMovementType.Cancellation,
                    Minutes = -movement.Minutes,
                    BalanceAfter = accumulation.AccumulatedMinutes,
                    SourceDate = movement.SourceDate,
                    OvertimePaymentRequestId = request.Id,
                    Notes = $"Cancelación de solicitud de pago: {reason}",
                    ByUserId = byUser,
                    RelatedMovementId = movement.Id,
                    CreatedAt = now
                });

                cancelledCount++;
            }

            // Eliminar la papeleta y su cadena de firmas materializada.
            var chain = await _context.absenceRequestApprovals
                .Where(a => a.RequestType == ApprovalRequestType.OvertimePayment && a.AbsenceRequestId == request.Id)
                .ToListAsync();
            if (chain.Count > 0)
            {
                _context.absenceRequestApprovals.RemoveRange(chain);
            }

            request.Status = AbsenceRequestStatus.Rejected;
            request.DeletedAt = now;
            request.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = $"Solicitud de pago eliminada. Se revirtieron {cancelledCount} pago(s); las horas volvieron a pendientes."
            };
        }

        /// <summary>
        /// Obtiene el historial de movimientos
        /// </summary>
        public async Task<OvertimeMovementsPagedOutput> GetMovementHistory(GetOvertimeMovementsInput input, int companyId)
        {
            var query = _context.overtimeMovementLogs
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.CompanyId == companyId);

            if (input.EmployeeCode.HasValue)
            {
                query = query.Where(m => m.EmployeeCode == input.EmployeeCode.Value);
            }

            if (input.StartDate.HasValue)
            {
                query = query.Where(m => m.SourceDate >= input.StartDate.Value);
            }

            if (input.EndDate.HasValue)
            {
                query = query.Where(m => m.SourceDate <= input.EndDate.Value);
            }

            if (input.MovementType.HasValue)
            {
                query = query.Where(m => m.MovementType == input.MovementType.Value);
            }

            var totalRecords = await query.CountAsync();

            var movements = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((input.Page - 1) * input.PageSize)
                .Take(input.PageSize)
                .ToListAsync();

            // Obtener cancelaciones relacionadas
            var movementIds = movements.Select(m => m.Id).ToList();
            var cancellations = await _context.overtimeMovementLogs
                .Where(m => m.RelatedMovementId != null && movementIds.Contains(m.RelatedMovementId.Value))
                .ToDictionaryAsync(m => m.RelatedMovementId!.Value, m => m.Id);

            // Obtener nombres de empleados
            var employeeCodes = movements.Select(m => m.EmployeeCode).Distinct().ToList();
            var employees = await GetEmployeesInfo(employeeCodes, companyId);

            var items = movements.Select(m =>
            {
                var hasCancellation = cancellations.TryGetValue(m.Id, out var cancellationId);
                employees.TryGetValue(m.EmployeeCode, out var emp);

                return new OvertimeMovementLogOutput
                {
                    Id = m.Id,
                    EmployeeCode = m.EmployeeCode,
                    EmployeeName = emp?.FullName ?? string.Empty,
                    MovementType = m.MovementType,
                    MovementTypeLabel = GetMovementTypeLabel(m.MovementType),
                    Minutes = m.Minutes,
                    MinutesFormatted = FormatMinutes(Math.Abs(m.Minutes)),
                    BalanceAfter = m.BalanceAfter,
                    BalanceAfterFormatted = FormatMinutes(m.BalanceAfter),
                    SourceDate = m.SourceDate,
                    AppliedRestDate = m.AppliedRestDate,
                    OriginalCheckIn = m.OriginalCheckIn,
                    OriginalCheckOut = m.OriginalCheckOut,
                    Notes = m.Notes,
                    CreatedByUser = m.User?.Name ?? string.Empty,
                    CreatedAt = m.CreatedAt,
                    IsCancelled = hasCancellation,
                    CancellationMovementId = hasCancellation ? cancellationId : null
                };
            }).ToList();

            return new OvertimeMovementsPagedOutput
            {
                Items = items,
                TotalRecords = totalRecords,
                Page = input.Page,
                PageSize = input.PageSize
            };
        }

        /// <summary>
        /// Procesa horas extras en lote para un período
        /// </summary>
        public async Task<List<OvertimeOperationResult>> ProcessOvertimesBatch(
            ProcessOvertimesBatchInput input,
            int companyId,
            string tenant,
            string? userId)
        {
            // Si es pago, validar que exista el formato de horas extras antes de procesar nada.
            if (!input.Accumulate)
            {
                await EnsureOvertimePaymentDocumentExists();
            }

            var summaries = await GetOvertimeSummary(input.TypeNomina, input.NumPeriod, companyId, tenant);
            var results = new List<OvertimeOperationResult>();

            foreach (var summary in summaries)
            {
                if (input.EmployeeCodes != null && input.EmployeeCodes.Any() &&
                    !input.EmployeeCodes.Contains(summary.EmployeeCode))
                {
                    continue;
                }

                // En pago, se acumulan los movimientos del empleado para generar UNA sola
                // papeleta por empleado (no se agrupan trabajadores).
                var paidMovementIds = new List<int>();
                var paidMinutes = 0;

                foreach (var day in summary.DayDetails.Where(d => d.Status == OvertimeDayStatus.Pending))
                {
                    OvertimeOperationResult result;

                    if (input.Accumulate)
                    {
                        result = await AccumulateOvertime(new AccumulateOvertimeInput
                        {
                            EmployeeCode = summary.EmployeeCode,
                            SourceDate = day.Date,
                            Minutes = day.OvertimeMinutes,
                            CheckIn = day.CheckIn,
                            CheckOut = day.CheckOut,
                            Notes = input.Notes ?? $"Procesamiento en lote - Período {input.NumPeriod}"
                        }, companyId, userId);
                    }
                    else
                    {
                        // createPaymentRequest: false -> la papeleta se crea una vez por empleado abajo.
                        result = await PayOvertimeDirect(new PayOvertimeDirectInput
                        {
                            EmployeeCode = summary.EmployeeCode,
                            SourceDate = day.Date,
                            Minutes = day.OvertimeMinutes,
                            CheckIn = day.CheckIn,
                            CheckOut = day.CheckOut,
                            Notes = input.Notes ?? $"Pago directo en lote - Período {input.NumPeriod}"
                        }, companyId, userId, createPaymentRequest: false);

                        if (result.Success && result.MovementId.HasValue)
                        {
                            paidMovementIds.Add(result.MovementId.Value);
                            paidMinutes += day.OvertimeMinutes;
                        }
                    }

                    results.Add(result);
                }

                // Una sola papeleta de pago por empleado, con todas sus horas pagadas en el lote.
                if (!input.Accumulate && paidMovementIds.Count > 0)
                {
                    await CreateOvertimePaymentRequest(summary.EmployeeCode, companyId, userId,
                        paidMovementIds, paidMinutes, input.Notes ?? $"Pago en lote - Período {input.NumPeriod}");
                }
            }

            return results;
        }

        /// <summary>
        /// Envía horas extras al banco de horas (reserva sin uso específico)
        /// </summary>
        public async Task<OvertimeOperationResult> SendToHourBank(SendToHourBankInput input, int companyId, string? userId)
        {
            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);

            accumulation.AccumulatedMinutes += input.Minutes;
            accumulation.UpdatedAt = DateTime.UtcNow;

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.HourBank,
                Minutes = input.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.SourceDate,
                OriginalCheckIn = input.CheckIn,
                OriginalCheckOut = input.CheckOut,
                Notes = input.Notes ?? "Enviado a Banco de Horas",
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Horas enviadas al banco correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Agrega un registro manual de horas extras (de sistema externo).
        /// No modifica el balance: el registro aparece como pendiente en el listado
        /// para que el usuario decida si acumular, pagar o enviar a banco de horas.
        /// </summary>
        public async Task<OvertimeOperationResult> AddManualEntry(ManualOvertimeEntryInput input, int companyId, string? userId)
        {
            // Verificar que no exista ya un registro externo para esa fecha y empleado
            var existingEntry = await _context.overtimeMovementLogs
                .AnyAsync(m =>
                    m.EmployeeCode == input.EmployeeCode &&
                    m.CompanyId == companyId &&
                    m.SourceDate == input.SourceDate &&
                    m.MovementType == OvertimeMovementType.ExternalEntry);

            if (existingEntry)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Ya existe un registro externo para este empleado en esta fecha."
                };
            }

            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);

            var notes = input.Notes ?? "";
            if (!string.IsNullOrWhiteSpace(input.ExternalReference))
            {
                notes = $"[Ref: {input.ExternalReference}] {notes}";
            }

            var movementLog = new OvertimeMovementLog
            {
                OvertimeAccumulationId = accumulation.Id,
                EmployeeCode = input.EmployeeCode,
                CompanyId = companyId,
                MovementType = OvertimeMovementType.ExternalEntry,
                Minutes = input.Minutes,
                BalanceAfter = accumulation.AccumulatedMinutes,
                SourceDate = input.SourceDate,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Registro externo de tiempo extra" : notes,
                ByUserId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                CreatedAt = DateTime.UtcNow
            };

            _context.overtimeMovementLogs.Add(movementLog);
            await _context.SaveChangesAsync();

            return new OvertimeOperationResult
            {
                Success = true,
                Message = "Registro externo agregado correctamente.",
                MovementId = movementLog.Id,
                NewBalance = accumulation.AccumulatedMinutes,
                NewBalanceFormatted = FormatMinutes(accumulation.AccumulatedMinutes)
            };
        }

        /// <summary>
        /// Verifica si hay empleados con horas extras pendientes en un periodo
        /// </summary>
        public async Task<(bool HasPending, int Count)> HasPendingOvertimes(
            int typeNomina, int numPeriod, int companyId, string? tenant)
        {
            var summaries = await GetOvertimeSummary(typeNomina, numPeriod, companyId, tenant);
            var pendingEmployees = summaries.Where(s => s.PendingMinutes > 0).ToList();
            return (pendingEmployees.Any(), pendingEmployees.Count);
        }

        #region Helper Methods

        private async Task<OvertimeAccumulation> GetOrCreateAccumulation(int employeeCode, int companyId)
        {
            var accumulation = await _context.overtimeAccumulations
                .FirstOrDefaultAsync(a => a.EmployeeCode == employeeCode && a.CompanyId == companyId);

            if (accumulation == null)
            {
                accumulation = new OvertimeAccumulation
                {
                    EmployeeCode = employeeCode,
                    CompanyId = companyId,
                    AccumulatedMinutes = 0,
                    UsedMinutes = 0,
                    PaidMinutes = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.overtimeAccumulations.Add(accumulation);
                await _context.SaveChangesAsync();
            }

            return accumulation;
        }

        private async Task<EmployeeBasicInfo?> GetEmployeeInfo(int employeeCode, int companyId)
        {
            var employees = await GetEmployeesInfo(new List<int> { employeeCode }, companyId);
            return employees.TryGetValue(employeeCode, out var emp) ? emp : null;
        }

        private async Task<Dictionary<int, EmployeeBasicInfo>> GetEmployeesInfo(List<int> employeeCodes, int companyId)
        {
            return await _keyRepository.GetContextEntity().AsNoTracking()
                .Where(k => k.Company == companyId && employeeCodes.Contains((int)k.Codigo))
                .Select(k => new EmployeeBasicInfo
                {
                    Code = (int)k.Codigo,
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName ?? string.Empty : string.Empty,
                    JobPosition = k.Tabulator.Activity ?? string.Empty
                })
                .ToDictionaryAsync(e => e.Code);
        }

        private static string FormatMinutes(int minutes)
        {
            var isNegative = minutes < 0;
            minutes = Math.Abs(minutes);
            var hours = minutes / 60;
            var mins = minutes % 60;
            var formatted = $"{hours} hrs {mins:D2} min";
            return isNegative ? $"-{formatted}" : formatted;
        }

        private static string GetStatusLabel(OvertimeDayStatus status) => status switch
        {
            OvertimeDayStatus.Pending => "Pendiente",
            OvertimeDayStatus.Accumulated => "Acumulado",
            OvertimeDayStatus.Paid => "Pagado",
            OvertimeDayStatus.Cancelled => "Cancelado",
            OvertimeDayStatus.HourBank => "Banco de Horas",
            _ => "Desconocido"
        };

        private static string GetMovementTypeLabel(OvertimeMovementType type) => type switch
        {
            OvertimeMovementType.Accumulation => "Acumulación",
            OvertimeMovementType.UsedForRestDay => "Día de descanso",
            OvertimeMovementType.DirectPayment => "Pago directo",
            OvertimeMovementType.ManualAdjustment => "Ajuste manual",
            OvertimeMovementType.Cancellation => "Cancelación",
            OvertimeMovementType.HourBank => "Banco de Horas",
            OvertimeMovementType.ExternalEntry => "Registro externo",
            OvertimeMovementType.UsedForTimeOff => "Usado en permiso",
            _ => "Desconocido"
        };

        #endregion
    }
}
