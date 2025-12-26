using ClosedXML.Excel;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services.Utilities;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace PrenominaApi.Services.Prenomina
{
    public class ClockService : ServicePrenomina<Clock>
    {
        private IBaseRepository<AttendanceRecords> _attendaceRecordRepository;
        private IBaseRepository<Employee> _employeeRepository;
        private IBaseRepositoryPrenomina<ClockAttendance> _clockAttendaceRepository;

        public ClockService(
            IBaseRepository<AttendanceRecords> attendaceRecordRepository,
            IBaseRepository<Employee> employeeRepository,
            IBaseRepositoryPrenomina<ClockAttendance> clockAttendaceRepository,
            IBaseRepositoryPrenomina<Clock> baseRepository
        ) : base(baseRepository) {
            _attendaceRecordRepository = attendaceRecordRepository;
            _employeeRepository = employeeRepository;
            _clockAttendaceRepository = clockAttendaceRepository;
        }

        public Clock ExecuteProcess(CreateClock createClock)
        {
            var existClock = _repository.GetByFilter(c => c.Ip == createClock.Ip).FirstOrDefault();

            if (existClock != null)
            {
                throw new BadHttpRequestException("La IP ya fue registrada en otro reloj.");
            }

            var result = _repository.Create(new Clock()
            {
                Ip = createClock.Ip,
                Label = createClock.Label,
                Port = createClock.Port ?? 4370
            });

            _repository.Save();

            return result;
        }

        public async Task<IEnumerable<ClockUser>> ExecuteProcess(GetClockUser getClockUser)
        {
            var result = new List<ClockUser>();
            var clock = _repository.GetById(getClockUser.Id);

            if (clock == null) {
                throw new BadHttpRequestException("El reloj no existe.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"tools/zkbridge/ZKBridgeApp.exe",
                    Arguments = $"{clock.Ip} {clock.Port ?? 4370} getusers",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            string output = ""; 
            string error = "";

            try {
                process.Start();

                output = await process.StandardOutput.ReadToEndAsync();
                error = await process.StandardError.ReadToEndAsync();

                output = ClearClockJsonResponse.OutputJson(output);

                process.WaitForExit();
            } catch (Exception)
            {
                throw new Exception($"Ocurrio un error en la comunicacion con el reloj");
            }

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Error: {error}");
            }

            var userClocks = JsonSerializer.Deserialize<List<ModelClock.User>>(output) ?? new List<ModelClock.User>();

            foreach (var user in userClocks)
            {
                var clockUser = new ClockUser()
                {
                    EnrollNumber = user.EnrollNumber,
                    Name = user.Name,
                    Password = user.Password,
                    Privilege = user.Privilege,
                    Enabled = user.Enabled,
                    UserFingers = new List<ClockUserFinger>(),
                };

                result.Add(clockUser);
            }

            return result;
        }

        public async Task<bool> ExecuteProcess(SyncClockUserToDB getClockUser)
        {
            var result = new List<ClockUser>();
            var clock = _repository.GetById(getClockUser.Id);

            if (clock == null)
            {
                throw new BadHttpRequestException("El reloj no existe.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"tools/zkbridge/ZKBridgeApp.exe",
                    Arguments = $"{clock.Ip} {clock.Port ?? 4370} getfullusers",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            string output = "";
            string error = "";

            try
            {
                process.Start();

                output = await process.StandardOutput.ReadToEndAsync();
                error = await process.StandardError.ReadToEndAsync();

                output = ClearClockJsonResponse.OutputJson(output);

                Log.Error("Result User", output);

                process.WaitForExit();
            }
            catch (Exception)
            {
                throw new Exception($"Ocurrio un error en la comunicacion con el reloj");
            }

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Error: {error}");
            }

            var userClocks = JsonSerializer.Deserialize<List<ModelClock.User>>(output) ?? new List<ModelClock.User>();

            var context = _repository.GetDbContext();

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var user in userClocks)
                    {
                        var clockUser = new ClockUser()
                        {
                            EnrollNumber = user.EnrollNumber,
                            Name = user.Name,
                            Password = user.Password,
                            Privilege = user.Privilege,
                            Enabled = user.Enabled,
                            CardNumber = user.CardNumber,
                            FaceBase64 = user.FaceBase64,
                            FaceLength = user.FaceLength,
                            UserFingers = new List<ClockUserFinger>(),
                        };

                        foreach (var finger in user.UserFingers)
                        {
                            (clockUser.UserFingers as List<ClockUserFinger>)!.Add(new ClockUserFinger()
                            {
                                EnrollNumber = finger.EnrollNumber,
                                FingerBase64 = finger.FingerBase64,
                                FingerIndex = finger.FingerIndex,
                                FingerLength = finger.FingerLength,
                                Flag = finger.Flag,
                                ClockUser = clockUser,
                            });
                        }

                        result.Add(clockUser);
                    }

                    var enrollmentCodes = result.Select(u => u.EnrollNumber).ToHashSet();
                    var existUserClocks = context.clockUsers.Where(cu => enrollmentCodes.Contains(cu.EnrollNumber)).ToList();

                    var resultDict = result.ToDictionary(u => u.EnrollNumber);

                    var enrollmentExist = existUserClocks.Select(u => u.EnrollNumber).ToHashSet();

                    var userClockToInsert = result.Where(u => !enrollmentExist.Contains(u.EnrollNumber)).ToList();

                    await context.clockUsers.AddRangeAsync(userClockToInsert);

                    foreach (var item in existUserClocks)
                    {
                        if (resultDict.TryGetValue(item.EnrollNumber, out var userFind))
                        {
                            item.Enabled = userFind.Enabled;
                            item.CardNumber = userFind.CardNumber;
                            item.FaceBase64 = userFind.FaceBase64;
                            item.FaceLength = userFind.FaceLength;
                            item.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return true;
        }

        public bool ExecuteProcess(PingToClock pingToClock)
        {
            try
            {
                using(Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(pingToClock.IP, 1000);

                    return reply.Status == IPStatus.Success;
                }
            } catch
            {
                return false;
            }
        }

        public async Task<bool> ExecuteProcess(SyncClockAttendance syncClockAttendace)
        {
            var result = new List<ClockAttendance>();
            var clock = _repository.GetById(syncClockAttendace.Id);

            if (clock == null)
            {
                throw new BadHttpRequestException("El reloj no existe.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"tools/zkbridge/ZKBridgeApp.exe",
                    Arguments = $"{clock.Ip} {clock.Port ?? 4370} getcheckins",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            string output = "";
            string error = "";

            try
            {
                process.Start();

                output = await process.StandardOutput.ReadToEndAsync();
                error = await process.StandardError.ReadToEndAsync();

                output = ClearClockJsonResponse.OutputJson(output);

                process.WaitForExit();
            }
            catch (Exception)
            {
                throw new Exception($"Ocurrio un error en la comunicacion con el reloj");
            }

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Error: {error}");
            }

            var checkins = JsonSerializer.Deserialize<List<ModelClock.CheckIn>>(output) ?? new List<ModelClock.CheckIn>();

            var context = _repository.GetDbContext();

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var check in checkins)
                    {
                        result.Add(new ClockAttendance()
                        {
                            EnrollNumber = check.EnrollNumber,
                            ClockId = clock.Id,
                            Day = check.Day,
                            Hour = check.Hour,
                            InOutMode = check.InOutMode,
                            Minute = check.Minute,
                            Month = check.Month,
                            Second = check.Second,
                            VerifyMode = check.VerifyMode,
                            WorkCode = check.WorkCode,
                            Year = check.Year,
                        });
                    }

                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE #TempEmployeeCheckIns (
                            id UNIQUEIDENTIFIER,
                            EmployeeCode INT,
                            CompanyId INT,
                            [Date] DATE,
                            CheckIn TIME,
                            EoS INT,
                            UpdatedAt DATETIME2
                        )");

                    List<string> enrollNumbers = result.Select(x => x.EnrollNumber).Distinct().ToList();
                    var listEmployesWithCompany = _employeeRepository.GetByFilter(e => enrollNumbers.Contains(e.Codigo.ToString()) && e.Active == 'S').Select(e =>
                    (
                        e.Codigo,
                        e.Company
                    )).ToList();

                    var newCheckIns = BuildCheckInsLogic(result, listEmployesWithCompany);
                    var table = BuildEmployeeCheckInsTable(newCheckIns);
                    await BulkInsertTempAsync(context, table);
                    await context.Database.ExecuteSqlRawAsync(@"
                        MERGE employee_check_ins AS target
                        USING #TempEmployeeCheckIns AS source
                        ON  target.employee_code = source.EmployeeCode
                        AND target.company_id = source.CompanyId
                        AND target.date = source.Date
                        AND target.check_in = source.CheckIn

                        WHEN MATCHED THEN
                            UPDATE SET
                                target.EoS = source.EoS,
                                target.updated_at = source.UpdatedAt

                        WHEN NOT MATCHED THEN
                            INSERT (
                                id,
                                employee_code,
                                company_id,
                                date,
                                check_in,
                                EoS,
                                updated_at,
                                period,
                                type_nom,
                                employee_schedule,
                                created_at
                            )
                            VALUES (
                                source.id,
                                source.EmployeeCode,
                                source.CompanyId,
                                source.Date,
                                source.CheckIn,
                                source.EoS,
                                source.UpdatedAt,
                                0,
                                0,
                                0,
                                GETDATE()
                            );
                    ");

                    // insert check to db
                    //await context.clockAttendances.AddRangeAsync(result);
                    //await context.SaveChangesAsync();


                    /*List<string> enrollNumbers = result.Select(x => x.EnrollNumber).Distinct().ToList();

                    if (enrollNumbers.Any())
                    {
                        List<DateOnly> dateRange = result.Select(r => new DateOnly(r.Year, r.Month, r.Day)).OrderBy(d => d).ToList();
                        DateOnly minDate = dateRange.Min().AddDays(-1);
                        DateOnly maxDate = dateRange.Max();

                        var listEmployesWithCompany = _employeeRepository.GetByFilter(e => enrollNumbers.Contains(e.Codigo.ToString()) && e.Active == 'S').Select(e => new
                        {
                            e.Codigo,
                            e.Company
                        }).ToList();
                        var existingCheckIns = await context.employeeCheckIns.Where(ci => enrollNumbers.Contains(ci.EmployeeCode.ToString()) && ci.Date >= minDate && ci.Date <= maxDate && ci.DeletedAt == null).ToListAsync();
                        var checkInsByEmployee = existingCheckIns.GroupBy(ci => ci.EmployeeCode).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Date).ThenBy(c => c.CheckIn).ToList());
                        var newCheckIns = new List<EmployeeCheckIns>();

                        //SET @tiponom = (SELECT tiponom FROM dbo.Llaves WHERE empresa=@empresa AND codigo=@codigo)
                        //SET @periodo = (SELECT MAX(periodo) FROM dbo.Periodos WHERE  empresa=@empresa AND tiponom=@tiponom AND @fecha BETWEEN inicio AND cierre)

                        foreach (var log in result)
                        {
                            var employWithCompany = listEmployesWithCompany.Where(e => e.Codigo.ToString() == log.EnrollNumber).FirstOrDefault();

                            if (employWithCompany == null)
                            {
                                continue;
                            }

                            var employeeCode = employWithCompany.Codigo;
                            var companyId = employWithCompany.Company;
                            var dateTime = new DateTime(log.Year, log.Month, log.Day, log.Hour, log.Minute, log.Second);
                            var dateOnly = DateOnly.FromDateTime(dateTime);
                            var timeOnly = TimeOnly.FromDateTime(dateTime);

                            if (!checkInsByEmployee.TryGetValue((int)employeeCode, out var employeeCheckIns))
                            {
                                employeeCheckIns = new List<EmployeeCheckIns>();
                                checkInsByEmployee[(int)employeeCode] = employeeCheckIns;
                            }

                            var lastCheckIn = employeeCheckIns.Where(c => c.CompanyId == companyId && (c.Date == dateOnly.AddDays(-1) || c.Date == dateOnly)).OrderBy(c => c.Date)
                                .ThenBy(c => c.CheckIn)
                                .LastOrDefault();

                            EntryOrExit eos;

                            if (lastCheckIn == null)
                            {
                                eos = EntryOrExit.Entry;
                            } else if (lastCheckIn.Date == dateOnly)
                            {
                                eos = EntryOrExit.Exit;
                            }
                            else
                            {
                                var lastCheckDateTime = lastCheckIn.Date.ToDateTime(lastCheckIn.CheckIn);
                                var diffHours = (dateTime - lastCheckDateTime).TotalHours;

                                eos = (lastCheckIn.EoS == EntryOrExit.Entry && diffHours <= 13) ? EntryOrExit.Exit : EntryOrExit.Entry;
                            }

                            var checkIn = new EmployeeCheckIns
                            {
                                EmployeeCode = (int)employeeCode,
                                CompanyId = companyId,
                                CheckIn = timeOnly,
                                Date = dateOnly,
                                NumConc = "",
                                EoS = eos,
                                Period = 0,
                                TypeNom = 0,
                                EmployeeSchedule = 0,
                                UpdatedAt = DateTime.UtcNow
                            };

                            newCheckIns.Add(checkIn);
                            employeeCheckIns.Add(checkIn);

                            //if (employWithCompany != null)
                            //{
                            //    var companyId = employWithCompany.Company;
                            //    var date = new DateTime(log.Year, log.Month, log.Day, log.Hour, log.Minute, log.Second);
                            //    var parseDate = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                            //    var time = date.ToString("HH:mm:ss.F", CultureInfo.CreateSpecificCulture("hr-HR"));
                            //    await _employeeRepository.GetDbContext().Database.ExecuteSqlRawAsync("EXEC ObtenRelojDatosEmps @empresa = {0}, @codigo = {1}, @fecha = {2}, @hora = {3}, @comedor = {4}, @row1 = {5}, @row2 = {6}", companyId, log.EnrollNumber, parseDate, time, "N", " ", " ");
                            //}
                        }

                        await context.employeeCheckIns.AddRangeAsync(newCheckIns);
                        await context.clockAttendances.AddRangeAsync(result);
                        await context.SaveChangesAsync();
                    }
                    */
                    //Clear Data
                    /*var processClear = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = @"tools/zkbridge/ZKBridgeApp.exe",
                            Arguments = $"{clock.Ip} {clock.Port ?? 4370} clearcheckins",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    processClear.Start();

                    string errorClear = await processClear.StandardError.ReadToEndAsync();

                    processClear.WaitForExit();

                    if (!string.IsNullOrEmpty(errorClear))
                    {
                        throw new Exception($"Error: {errorClear}");
                    }*/

                    await transaction.CommitAsync();

                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();

                    throw;
                }
                
            }
            
            return true;
        }

        public async Task<ResultImportCheckinsFromFile> ExecuteProcess(ImportCheckinsFromFile importCheckinsFromFile)
        {
            var listCheckins = new List<EmployeeCheckIns>();
            var result = new ResultImportCheckinsFromFile()
            {
                totalErrors = 0,
                totalImported = 0,
            };

            if (importCheckinsFromFile.File == null || importCheckinsFromFile.File.Length == 0)
            {
                throw new BadHttpRequestException("Archivo no válido.");
            }

            var extension = Path.GetExtension(importCheckinsFromFile.File.FileName).ToLower();

            if (extension != ".xlsx" && extension != ".csv")
            {
                throw new BadHttpRequestException("Formato de archivo no soportado, Formatos permitidos .xlsx o .csv.");
            }

            var context = _repository.GetDbContext();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await importCheckinsFromFile.File.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".xlxs")
                    {
                        using (var workbook = new XLWorkbook(stream))
                        {
                            var worksheet = workbook.Worksheet(1);
                            foreach (var row in worksheet.RowsUsed())
                            {
                                var employeeCode = (int)row.Cell(1).Value.GetNumber();
                                var checkin = TimeOnly.Parse(row.Cell(2).Value.GetText());
                                var date = DateOnly.Parse(row.Cell(3).Value.GetText());

                                listCheckins.Add(new EmployeeCheckIns()
                                {
                                    EmployeeCode = employeeCode,
                                    Date = date,
                                    CheckIn = checkin
                                });
                            }
                        }
                    } else if (extension == ".csv")
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            //await reader.ReadLineAsync();
                            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                            csv.Read();

                            while (csv.Read())
                            {
                                var employeeCode = csv.GetField<int>(0);
                                var parseCheckin = TimeOnly.TryParse(csv.GetField<string>(1), out var checkin);

                                if (!parseCheckin)
                                {
                                    result.totalErrors += 1;

                                    continue;
                                }

                                var parseDate = DateTime.TryParse(csv.GetField<string>(2), out var date);

                                if (!parseDate)
                                {
                                    result.totalErrors += 1;

                                    continue;
                                }

                                listCheckins.Add(new EmployeeCheckIns()
                                {
                                    EmployeeCode = employeeCode,
                                    Date = DateOnly.FromDateTime(date),
                                    CheckIn = checkin
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException($"Error al procesar el archivo: {ex.Message}");
            }

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (listCheckins.Any())
                    {
                        List<int> enrollNumbers = listCheckins.Select(c => c.EmployeeCode).Distinct().ToList();
                        List<DateOnly> dateRange = listCheckins.Select(r => r.Date).OrderBy(d => d).ToList();
                        DateOnly minDate = dateRange.Min().AddDays(-1);
                        DateOnly maxDate = dateRange.Max();

                        var listEmployesWithCompany = _employeeRepository.GetByFilter(e => enrollNumbers.Contains((int)e.Codigo) && e.Active == 'S').Select(e => new
                        {
                            e.Codigo,
                            e.Company
                        }).ToList();
                        var existingCheckIns = await context.employeeCheckIns.Where(ci => enrollNumbers.Contains(ci.EmployeeCode) && ci.Date >= minDate && ci.Date <= maxDate && ci.DeletedAt == null).ToListAsync();
                        var checkInsByEmployee = existingCheckIns.GroupBy(ci => ci.EmployeeCode).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Date).ThenBy(c => c.CheckIn).ToList());
                        var newCheckIns = new List<EmployeeCheckIns>();

                        foreach (var log in listCheckins)
                        {
                            var employWithCompany = listEmployesWithCompany.Where(e => e.Codigo == log.EmployeeCode).FirstOrDefault();

                            if (employWithCompany == null)
                            {
                                continue;
                            }

                            var employeeCode = employWithCompany.Codigo;
                            var companyId = employWithCompany.Company;
                            var dateOnly = log.Date;
                            var timeOnly = log.CheckIn;
                            var dateTime = dateOnly.ToDateTime(timeOnly);

                            if (!checkInsByEmployee.TryGetValue((int)employeeCode, out var employeeCheckIns))
                            {
                                employeeCheckIns = new List<EmployeeCheckIns>();
                                checkInsByEmployee[(int)employeeCode] = employeeCheckIns;
                            }

                            var lastCheckIn = employeeCheckIns.Where(c => c.CompanyId == companyId && (c.Date == dateOnly.AddDays(-1) || c.Date == dateOnly)).OrderBy(c => c.Date)
                                .ThenBy(c => c.CheckIn)
                                .LastOrDefault();

                            EntryOrExit eos;

                            if (employeeCode == 11 && dateOnly.ToString() == "11/06/2025")
                            {
                                Console.WriteLine("Hola");
                            }

                            if (employeeCode == 93 && dateOnly.ToString() == "26/11/2025")
                            {
                                Console.WriteLine("line");
                            }

                            if (lastCheckIn == null)
                            {
                                eos = EntryOrExit.Entry;
                            } else if (lastCheckIn.Date == dateOnly)
                            {
                                eos = EntryOrExit.Exit;
                            }
                            else
                            {
                                var lastCheckDateTime = lastCheckIn.Date.ToDateTime(lastCheckIn.CheckIn);
                                var diffHours = (dateTime - lastCheckDateTime).TotalHours;

                                eos = (lastCheckIn.EoS == EntryOrExit.Entry && diffHours <= 13) ? EntryOrExit.Exit : EntryOrExit.Entry;
                            }

                            var checkIn = new EmployeeCheckIns
                            {
                                EmployeeCode = (int)employeeCode,
                                CompanyId = companyId,
                                CheckIn = timeOnly,
                                Date = dateOnly,
                                NumConc = "",
                                EoS = eos,
                                Period = 0,
                                TypeNom = 0,
                                EmployeeSchedule = 0,
                                UpdatedAt = DateTime.UtcNow
                            };

                            newCheckIns.Add(checkIn);
                            employeeCheckIns.Add(checkIn);
                        }

                        await context.employeeCheckIns.AddRangeAsync(newCheckIns);
                        await context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception) {
                    await transaction.RollbackAsync();

                    throw;
                }
            }

            result.totalImported = listCheckins.Count;

            return result;
        }

        public async Task<bool> ExecuteProcess(SyncAllClocksAttendance syncAllClocksAttendance)
        {
            var result = new List<ClockAttendance>();
            var clocks = _repository.GetAll();

            foreach (var clock in clocks)
            {
                try
                {
                    await ExecuteProcess<SyncClockAttendance, Task<bool>>(new SyncClockAttendance()
                    {
                        Id = clock.Id
                    });
                } catch (Exception err)
                {
                    Log.Error($"Job: Ocurrio un error al sincronizar las checadas del reloj {clock.Label} - {err.Message}");
                    continue;
                }
            }

            return true;
        }
    
        private List<EmployeeCheckIns> BuildCheckInsLogic(List<ClockAttendance> logs, List<(decimal Codigo, decimal Company)> employees)
        {
            //var employeeMap = employees.ToDictionary(e => e.Codigo.ToString(), e => e.Company);

            var orderedLogs = logs//.Where(l => employeeMap.ContainsKey(l.EnrollNumber))
                .OrderBy(l => l.EnrollNumber)
                .ThenBy(l => l.Year)
                .ThenBy(l => l.Month)
                .ThenBy(l => l.Day)
                .ThenBy(l => l.Hour)
                .ThenBy(l => l.Minute)
                .ThenBy(l => l.Second)
                .ToList();

            var result = new List<EmployeeCheckIns>(orderedLogs.Count);

            var lastCheckMap = new Dictionary<(decimal Employee, decimal Company), EmployeeCheckIns>();

            foreach (var log in orderedLogs)
            {
                var employeeCode = decimal.Parse(log.EnrollNumber);
                var companyId = 7;//employeeMap[log.EnrollNumber];

                var currentDateTime = new DateTime(log.Year, log.Month, log.Day, log.Hour, log.Minute, log.Second);
                var dateOnly = DateOnly.FromDateTime(currentDateTime);
                var timeOnly = TimeOnly.FromDateTime(currentDateTime);

                var key = (employeeCode, companyId);
                EntryOrExit eos;

                if (!lastCheckMap.TryGetValue(key, out var last))
                {
                    eos = EntryOrExit.Entry;
                } else
                {
                    var lastDateTime = last.Date.ToDateTime(last.CheckIn);
                    var diffHours = (currentDateTime - lastDateTime).TotalHours;

                    if (last.Date == dateOnly)
                    {
                        eos = EntryOrExit.Exit;
                    }
                    else if (last.EoS == EntryOrExit.Entry && diffHours <= 13)
                    {
                        eos = EntryOrExit.Exit;
                    } else
                    {
                        eos = EntryOrExit.Entry;
                    }
                }

                var checkIn = new EmployeeCheckIns
                {
                    EmployeeCode = (int)employeeCode,
                    CompanyId = companyId,
                    Date = dateOnly,
                    CheckIn = timeOnly,
                    EoS = eos,
                    NumConc = "",
                    Period = 0,
                    TypeNom = 0,
                    EmployeeSchedule = 0,
                    UpdatedAt = DateTime.UtcNow
                };

                result.Add(checkIn);
                lastCheckMap[key] = checkIn;
            }

            return result;
        }
    
        private DataTable BuildEmployeeCheckInsTable(List<EmployeeCheckIns> items)
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(Guid)).DefaultValue = Guid.NewGuid();
            table.Columns.Add("EmployeeCode", typeof(int));
            table.Columns.Add("CompanyId", typeof(int));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("CheckIn", typeof(TimeSpan));
            table.Columns.Add("EoS", typeof(int));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var item in items)
            {
                table.Rows.Add(
                    item.Id,
                    item.EmployeeCode,
                    item.CompanyId,
                    item.Date.ToDateTime(TimeOnly.MinValue),
                    item.CheckIn.ToTimeSpan(),
                    (int)item.EoS,
                    item.UpdatedAt
                );
            }

            return table;
        }

        private async Task BulkInsertTempAsync(DbContext context, DataTable table, CancellationToken ct = default)
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();

            using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, (SqlTransaction)context.Database.CurrentTransaction!.GetDbTransaction())
            {
                DestinationTableName = "#TempEmployeeCheckIns",
                BatchSize = 5000
            };

            await bulk.WriteToServerAsync(table, ct);
        }
    }
}
