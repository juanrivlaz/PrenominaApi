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

        public OvertimeAccumulationService(
            PrenominaDbContext context,
            IBaseRepository<Key> keyRepository,
            GlobalPropertyService globalPropertyService,
            IBaseServicePrenomina<Period> periodService)
        {
            _context = context;
            _keyRepository = keyRepository;
            _globalPropertyService = globalPropertyService;
            _periodService = periodService;
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
                    FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
                    Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty,
                    JobPosition = k.Tabulator.Activity
                })
                .ToListAsync();

            if (!employees.Any())
            {
                return new List<OvertimeSummaryOutput>();
            }

            // Filtrar empleados excluidos de horas extras
            var allCodes = employees.Select(e => (int)e.Codigo).ToList();
            var excludedCodes = await _context.employeeOvertimeConfigs
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && allCodes.Contains(c.EmployeeCode) && c.ExcludeOvertime)
                .Select(c => c.EmployeeCode)
                .ToListAsync();

            if (excludedCodes.Any())
            {
                var excludedSet = new HashSet<int>(excludedCodes);
                employees = employees.Where(e => !excludedSet.Contains((int)e.Codigo)).ToList();
            }

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

            // Obtener movimientos existentes en el período
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
            var existingMovementIds = existingMovements.Select(m => m.Id).ToList();
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

            // Obtener balances actuales
            var balances = await _context.overtimeAccumulations
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId && employeeCodes.Contains(a.EmployeeCode))
                .ToDictionaryAsync(a => a.EmployeeCode, a => a.AccumulatedMinutes);

            var movementsByEmployeeDate = existingMovements
                .GroupBy(m => (m.EmployeeCode, m.SourceDate))
                .ToDictionary(g => g.Key, g => g.First());

            var result = new List<OvertimeSummaryOutput>();

            foreach (var emp in employees)
            {
                var empOvertimes = overtimeData.Where(o => o.EmployeeCode == emp.Codigo).ToList();

                if (!empOvertimes.Any())
                {
                    continue;
                }

                var dayDetails = new List<OvertimeDayDetail>();
                int accumulatedInPeriod = 0;
                int paidInPeriod = 0;
                int pendingInPeriod = 0;

                foreach (var day in empOvertimes)
                {
                    var overtimeMinutes = day.TotalMinutesWorked - (8 * 60);
                    var status = OvertimeDayStatus.Pending;
                    int? movementId = null;

                    if (movementsByEmployeeDate.TryGetValue((day.EmployeeCode, day.Date), out var movement))
                    {
                        // Si el movimiento fue cancelado, el día vuelve a Pending
                        if (cancelledSet.Contains(movement.Id))
                        {
                            status = OvertimeDayStatus.Pending;
                            pendingInPeriod += overtimeMinutes;
                        }
                        else
                        {
                            movementId = movement.Id;
                            status = movement.MovementType switch
                            {
                                OvertimeMovementType.Accumulation => OvertimeDayStatus.Accumulated,
                                OvertimeMovementType.DirectPayment => OvertimeDayStatus.Paid,
                                OvertimeMovementType.HourBank => OvertimeDayStatus.HourBank,
                                _ => OvertimeDayStatus.Pending
                            };

                            if (status == OvertimeDayStatus.Accumulated)
                                accumulatedInPeriod += overtimeMinutes;
                            else if (status == OvertimeDayStatus.Paid)
                                paidInPeriod += overtimeMinutes;
                            else if (status == OvertimeDayStatus.Pending)
                                pendingInPeriod += overtimeMinutes;
                        }
                    }
                    else
                    {
                        pendingInPeriod += overtimeMinutes;
                    }

                    dayDetails.Add(new OvertimeDayDetail
                    {
                        Date = day.Date,
                        CheckIn = day.CheckIn,
                        CheckOut = day.CheckOut,
                        TotalMinutesWorked = day.TotalMinutesWorked,
                        OvertimeMinutes = overtimeMinutes,
                        OvertimeFormatted = FormatMinutes(overtimeMinutes),
                        Status = status,
                        StatusLabel = GetStatusLabel(status),
                        MovementId = movementId
                    });
                }

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
            // Verificar que no exista un movimiento para esa fecha
            var existingMovement = await _context.overtimeMovementLogs
                .AnyAsync(m =>
                    m.EmployeeCode == input.EmployeeCode &&
                    m.CompanyId == companyId &&
                    m.SourceDate == input.SourceDate &&
                    (m.MovementType == OvertimeMovementType.Accumulation ||
                     m.MovementType == OvertimeMovementType.DirectPayment));

            if (existingMovement)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Ya existe un movimiento registrado para esta fecha."
                };
            }

            // Obtener o crear acumulación
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
        public async Task<OvertimeOperationResult> PayOvertimeDirect(PayOvertimeDirectInput input, int companyId, string? userId)
        {
            var existingMovement = await _context.overtimeMovementLogs
                .AnyAsync(m =>
                    m.EmployeeCode == input.EmployeeCode &&
                    m.CompanyId == companyId &&
                    m.SourceDate == input.SourceDate &&
                    (m.MovementType == OvertimeMovementType.Accumulation ||
                     m.MovementType == OvertimeMovementType.DirectPayment));

            if (existingMovement)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Ya existe un movimiento registrado para esta fecha."
                };
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
            var summaries = await GetOvertimeSummary(input.TypeNomina, input.NumPeriod, companyId, tenant);
            var results = new List<OvertimeOperationResult>();

            foreach (var summary in summaries)
            {
                if (input.EmployeeCodes != null && input.EmployeeCodes.Any() &&
                    !input.EmployeeCodes.Contains(summary.EmployeeCode))
                {
                    continue;
                }

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
                        result = await PayOvertimeDirect(new PayOvertimeDirectInput
                        {
                            EmployeeCode = summary.EmployeeCode,
                            SourceDate = day.Date,
                            Minutes = day.OvertimeMinutes,
                            CheckIn = day.CheckIn,
                            CheckOut = day.CheckOut,
                            Notes = input.Notes ?? $"Pago directo en lote - Período {input.NumPeriod}"
                        }, companyId, userId);
                    }

                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Envía horas extras al banco de horas (reserva sin uso específico)
        /// </summary>
        public async Task<OvertimeOperationResult> SendToHourBank(SendToHourBankInput input, int companyId, string? userId)
        {
            var existingMovement = await _context.overtimeMovementLogs
                .AnyAsync(m =>
                    m.EmployeeCode == input.EmployeeCode &&
                    m.CompanyId == companyId &&
                    m.SourceDate == input.SourceDate &&
                    (m.MovementType == OvertimeMovementType.Accumulation ||
                     m.MovementType == OvertimeMovementType.DirectPayment ||
                     m.MovementType == OvertimeMovementType.HourBank));

            if (existingMovement)
            {
                return new OvertimeOperationResult
                {
                    Success = false,
                    Message = "Ya existe un movimiento registrado para esta fecha."
                };
            }

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
        /// Agrega un registro manual de horas extras (de sistema externo)
        /// </summary>
        public async Task<OvertimeOperationResult> AddManualEntry(ManualOvertimeEntryInput input, int companyId, string? userId)
        {
            var accumulation = await GetOrCreateAccumulation(input.EmployeeCode, companyId);

            accumulation.AccumulatedMinutes += input.Minutes;
            accumulation.UpdatedAt = DateTime.UtcNow;

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
                MovementType = OvertimeMovementType.ManualAdjustment,
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
            _ => "Desconocido"
        };

        #endregion
    }
}
