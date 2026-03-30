using PrenominaApi.Services.Prenomina;
using Serilog;

namespace PrenominaApi.Jobs
{
    public class BioTimeSyncJob : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isRunning = false;
        private DateOnly _lastExecutionDate = DateOnly.MinValue;

        public BioTimeSyncJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Verificar cada minuto si es hora de ejecutar
            _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            if (_isRunning) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<BioTimeSyncService>();

                var config = await syncService.GetSyncConfig();
                if (config == null || !config.Enabled) return;

                // Parsear la hora configurada
                if (!TimeOnly.TryParse(config.SyncHour, out var syncTime)) return;

                var now = DateTime.Now;
                var today = DateOnly.FromDateTime(now);
                var currentTime = TimeOnly.FromDateTime(now);

                // Si ya se ejecutó hoy, no repetir
                if (_lastExecutionDate == today) return;

                // Verificar si la hora actual es >= la hora configurada
                if (currentTime < syncTime) return;

                _isRunning = true;
                Log.Information("BioTimeSyncJob: Ejecutando sincronización programada a las {Hour}", config.SyncHour);

                await syncService.SyncYesterdayAttendance();

                _lastExecutionDate = today;
                Log.Information("BioTimeSyncJob: Sincronización completada");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BioTimeSyncJob: Error en sincronización programada");
            }
            finally
            {
                _isRunning = false;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
