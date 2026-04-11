using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto.BioTime;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace PrenominaApi.Services.Prenomina
{
    public class BioTimeSyncService
    {
        private readonly PrenominaDbContext _context;
        private readonly IBaseRepository<Key> _keyRepository;
        private readonly IDataProtector _protector;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ProtectorPurpose = "BioTimeCredentials";

        public BioTimeSyncService(
            PrenominaDbContext context,
            IBaseRepository<Key> keyRepository,
            IDataProtectionProvider dataProtectionProvider,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _keyRepository = keyRepository;
            _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Ejecuta la sincronización de checadas del día anterior
        /// </summary>
        public async Task<bool> SyncYesterdayAttendance()
        {
            var config = await GetSyncConfig();
            if (config == null || !config.Enabled)
            {
                Log.Information("BioTimeSync: Sincronización deshabilitada");
                return false;
            }

            var credentials = await GetCredentials();
            if (credentials == null)
            {
                Log.Warning("BioTimeSync: No se encontraron credenciales configuradas");
                return false;
            }

            var yesterday = DateTime.Now.AddDays(-1);
            var dateStr = yesterday.ToString("yyyy-MM-dd");

            Log.Information("BioTimeSync: Iniciando sincronización para fecha {Date}", dateStr);

            try
            {
                var token = await Authenticate(credentials);
                if (string.IsNullOrEmpty(token))
                {
                    Log.Error("BioTimeSync: No se pudo autenticar con BioTime");
                    return false;
                }

                var allRecords = await FetchAllPages(credentials.GetApiBaseUrl(), token, dateStr);
                if (!allRecords.Any())
                {
                    Log.Information("BioTimeSync: No se encontraron registros para {Date}", dateStr);
                    return true;
                }

                await ProcessAndInsertRecords(allRecords, yesterday);

                Log.Information("BioTimeSync: Sincronización completada. {Count} registros procesados", allRecords.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BioTimeSync: Error durante la sincronización");
                return false;
            }
        }

        /// <summary>
        /// Autentica con el API de BioTime y obtiene el JWT token
        /// </summary>
        private async Task<string?> Authenticate(BioTimeCredentials credentials)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var authBody = new BioTimeAuthRequest
            {
                Email = credentials.Email,
                Password = credentials.Password,
                Company = credentials.Company
            };

            var json = System.Text.Json.JsonSerializer.Serialize(authBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{credentials.GetApiBaseUrl()}/jwt-api-token-auth/", content);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("BioTimeSync: Error de autenticación. Status: {Status}", response.StatusCode);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var authResponse = System.Text.Json.JsonSerializer.Deserialize<BioTimeAuthResponse>(responseBody);
            return authResponse?.Token;
        }

        /// <summary>
        /// Obtiene todos los registros paginados del API
        /// </summary>
        private async Task<List<BioTimeAttendanceRecord>> FetchAllPages(string baseUrl, string token, string date)
        {
            var allRecords = new List<BioTimeAttendanceRecord>();
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("JWT", token);

            int page = 1;
            bool hasMore = true;

            while (hasMore)
            {
                var url = $"{baseUrl}/att/api/totalTimeCardReport/?start_date={date}&end_date={date}&page={page}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("BioTimeSync: Error al obtener página {Page}. Status: {Status}", page, response.StatusCode);
                    break;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var pagedResponse = System.Text.Json.JsonSerializer.Deserialize<BioTimePagedResponse>(responseBody);

                if (pagedResponse?.Data != null && pagedResponse.Data.Any())
                {
                    allRecords.AddRange(pagedResponse.Data);
                }

                hasMore = !string.IsNullOrEmpty(pagedResponse?.Next);
                page++;
            }

            return allRecords;
        }

        /// <summary>
        /// Procesa los registros de BioTime e inserta en employee_check_ins
        /// usando la misma lógica de MERGE que ClockService
        /// </summary>
        private async Task ProcessAndInsertRecords(List<BioTimeAttendanceRecord> records, DateTime syncDate)
        {
            // Obtener todos los códigos de empleados del API
            var empCodes = records.Select(r => r.EmpCode).Distinct().ToList();

            // Buscar empleados activos en la base de datos
            var employees = _keyRepository.GetContextEntity().Include(k => k.Employee).Where(e =>
                empCodes.Contains(e.Codigo.ToString()) && e.Employee.Active == 'S')
                .Select(e => new { e.Codigo, e.Company })
                .ToList();

            var employeeMap = employees.ToDictionary(e => e.Codigo.ToString(), e => e.Company);

            // Convertir registros de BioTime a EmployeeCheckIns
            var checkIns = new List<EmployeeCheckIns>();

            foreach (var record in records)
            {
                if (!employeeMap.ContainsKey(record.EmpCode))
                    continue;

                var companyId = employeeMap[record.EmpCode];
                var employeeCode = int.Parse(record.EmpCode);

                // Parsear fecha (formato dd-MM-yyyy)
                if (!DateOnly.TryParseExact(record.AttDate, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var dateOnly))
                    continue;

                // Insertar check_in como Entry
                if (!string.IsNullOrWhiteSpace(record.CheckIn) && TimeOnly.TryParse(record.CheckIn, out var checkIn))
                {
                    checkIns.Add(new EmployeeCheckIns
                    {
                        Id = Guid.NewGuid(),
                        EmployeeCode = employeeCode,
                        CompanyId = companyId,
                        Date = dateOnly,
                        CheckIn = checkIn,
                        SourceCheckIn = checkIn,
                        EoS = EntryOrExit.Entry,
                        NumConc = "",
                        Period = 0,
                        TypeNom = 0,
                        EmployeeSchedule = 0,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // Insertar check_out como Exit
                if (!string.IsNullOrWhiteSpace(record.CheckOut) && TimeOnly.TryParse(record.CheckOut, out var checkOut))
                {
                    checkIns.Add(new EmployeeCheckIns
                    {
                        Id = Guid.NewGuid(),
                        EmployeeCode = employeeCode,
                        CompanyId = companyId,
                        Date = dateOnly,
                        CheckIn = checkOut,
                        SourceCheckIn = checkOut,
                        EoS = EntryOrExit.Exit,
                        NumConc = "",
                        Period = 0,
                        TypeNom = 0,
                        EmployeeSchedule = 0,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (!checkIns.Any())
            {
                Log.Information("BioTimeSync: No hay registros válidos para insertar");
                return;
            }

            // Usar misma lógica de MERGE que ClockService
            await UpsertCheckIns(checkIns);
        }

        /// <summary>
        /// Inserta/actualiza check-ins usando temp table + MERGE (mismo patrón que ClockService)
        /// </summary>
        private async Task UpsertCheckIns(List<EmployeeCheckIns> checkIns)
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE #TempBioTimeCheckIns (
                        id UNIQUEIDENTIFIER,
                        EmployeeCode INT,
                        CompanyId INT,
                        [Date] DATE,
                        CheckIn TIME,
                        SourceCheckIn TIME,
                        EoS INT,
                        UpdatedAt DATETIME2
                    )");

                // Bulk insert a tabla temporal
                var table = BuildDataTable(checkIns);
                using (var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction.GetDbTransaction()))
                {
                    bulkCopy.DestinationTableName = "#TempBioTimeCheckIns";
                    bulkCopy.BatchSize = 5000;
                    await bulkCopy.WriteToServerAsync(table);
                }

                // MERGE: upsert a employee_check_ins
                await _context.Database.ExecuteSqlRawAsync(@"
                    MERGE employee_check_ins AS target
                    USING #TempBioTimeCheckIns AS source
                    ON  target.employee_code = source.EmployeeCode
                    AND target.company_id = source.CompanyId
                    AND target.date = source.Date
                    AND target.source_check_in = source.SourceCheckIn

                    WHEN MATCHED THEN
                        UPDATE SET
                            target.EoS = source.EoS,
                            target.updated_at = source.UpdatedAt

                    WHEN NOT MATCHED THEN
                        INSERT (id, employee_code, company_id, date, check_in, source_check_in, EoS, updated_at, period, type_nom, employee_schedule, created_at)
                        VALUES (source.id, source.EmployeeCode, source.CompanyId, source.Date, source.CheckIn, source.SourceCheckIn, source.EoS, source.UpdatedAt, 0, 0, 0, GETDATE());

                    DROP TABLE #TempBioTimeCheckIns;
                ");

                await transaction.CommitAsync();
                Log.Information("BioTimeSync: {Count} registros insertados/actualizados", checkIns.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private System.Data.DataTable BuildDataTable(List<EmployeeCheckIns> items)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("id", typeof(Guid));
            table.Columns.Add("EmployeeCode", typeof(int));
            table.Columns.Add("CompanyId", typeof(int));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("CheckIn", typeof(TimeSpan));
            table.Columns.Add("SourceCheckIn", typeof(TimeSpan));
            table.Columns.Add("EoS", typeof(int));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var item in items)
            {
                table.Rows.Add(
                    item.Id,
                    item.EmployeeCode,
                    (int)item.CompanyId,
                    item.Date.ToDateTime(TimeOnly.MinValue),
                    item.CheckIn.ToTimeSpan(),
                    item.SourceCheckIn.ToTimeSpan(),
                    (int)item.EoS,
                    item.UpdatedAt
                );
            }

            return table;
        }

        #region Configuración y Credenciales

        public async Task<SysBioTimeSyncConfig?> GetSyncConfig()
        {
            var setting = await _context.systemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.Key == SysConfig.BioTimeSync);

            if (setting == null) return null;
            return JsonConvert.DeserializeObject<SysBioTimeSyncConfig>(setting.Data);
        }

        public async Task SaveSyncConfig(SysBioTimeSyncConfig config)
        {
            var setting = await _context.systemConfigs
                .FirstOrDefaultAsync(sc => sc.Key == SysConfig.BioTimeSync);

            var json = JsonConvert.SerializeObject(config);

            if (setting == null)
            {
                _context.systemConfigs.Add(new SystemConfig
                {
                    Key = SysConfig.BioTimeSync,
                    Data = json,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                setting.Data = json;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BioTimeCredentials?> GetCredentials()
        {
            var setting = await _context.systemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.Key == SysConfig.BioTimeCredentials);

            if (setting == null) return null;

            try
            {
                var decrypted = _protector.Unprotect(setting.Data);
                return JsonConvert.DeserializeObject<BioTimeCredentials>(decrypted);
            }
            catch
            {
                Log.Warning("BioTimeSync: No se pudieron descifrar las credenciales");
                return null;
            }
        }

        public async Task SaveCredentials(BioTimeCredentials credentials)
        {
            var json = JsonConvert.SerializeObject(credentials);
            var encrypted = _protector.Protect(json);

            var setting = await _context.systemConfigs
                .FirstOrDefaultAsync(sc => sc.Key == SysConfig.BioTimeCredentials);

            if (setting == null)
            {
                _context.systemConfigs.Add(new SystemConfig
                {
                    Key = SysConfig.BioTimeCredentials,
                    Data = encrypted,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                setting.Data = encrypted;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
