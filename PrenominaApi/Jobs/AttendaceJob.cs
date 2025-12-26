using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Prenomina;
using Serilog;

namespace PrenominaApi.Jobs
{
    public class AttendaceJob : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private int _currentIntervalInMinutes = 1;

        public AttendaceJob(
            IServiceScopeFactory scopeFactory
        ) {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateIntervalFromDbAsync();
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(_currentIntervalInMinutes));
        }

        private async void DoWork(object? state)
        {
            Log.Information("Schedule: Extracción de checadas");
            using var scope = _scopeFactory.CreateScope();
            var clockService = scope.ServiceProvider.GetRequiredService<IBaseServicePrenomina<Clock>>();

            if (clockService != null)
            {
                await clockService.ExecuteProcess<SyncAllClocksAttendance, Task<bool>>(new SyncAllClocksAttendance());
            }

            Log.Information("Schedule: Finalizo la extracción de checadas");

            await UpdateIntervalFromDbAsync();
            _timer?.Change(TimeSpan.FromMinutes(_currentIntervalInMinutes), TimeSpan.FromMinutes(_currentIntervalInMinutes));
        }

        private async Task UpdateIntervalFromDbAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContex = scope.ServiceProvider.GetRequiredService<PrenominaDbContext>();
            var setting = await dbContex.systemConfigs.FirstAsync(sc => sc.Key == SysConfig.ExtractChecks);
            if (setting != null)
            {
                _currentIntervalInMinutes = JsonConvert.DeserializeObject<SysExtractCheck>(setting.Data)?.IntervalInMinutes ?? _currentIntervalInMinutes;
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
