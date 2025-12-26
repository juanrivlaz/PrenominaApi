using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Utilities;
using System.Diagnostics;

namespace PrenominaApi.Jobs
{
    public class ClockJob : IHostedService, IDisposable
    {
        private Timer? _timer;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWorkAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }
        private async void DoWorkAsync(object? state)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"tools/zkbridge/ZKBridgeApp.exe",
                    Arguments = "0 0 cleanup",
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
