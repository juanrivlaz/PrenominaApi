using System.Windows;
using System.Windows.Controls;
using PrenominaInstaller.Models;
using PrenominaInstaller.Services;

namespace PrenominaInstaller;

public partial class MainWindow : Window
{
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        // Sugerir la URL del API según el nombre de la máquina (visible para los navegadores en LAN).
        TxtApiUrl.Text = $"http://{Environment.MachineName}:5000/api";

        if (!InstallerService.PayloadExists)
        {
            AppendLog("⚠ No se encontró el API empaquetado en la carpeta 'api'.");
            AppendLog("  Ejecute build-installer.ps1 antes de usar el instalador (consulte el README).");
            BtnInstall.IsEnabled = false;
        }
        else
        {
            AppendLog("Listo. Captura los valores y presiona \"Instalar\".");
            if (ChkInstallWeb.IsChecked == true && !InstallerService.WebPayloadExists)
                AppendLog("⚠ No se encontró el frontend empaquetado (carpetas 'web' / 'webroot'). " +
                          "Desmarca \"Instalar frontend\" o ejecuta build-installer.ps1.");
        }
    }

    private void OnToggleWeb(object sender, RoutedEventArgs e)
    {
        if (PanelWeb != null)
            PanelWeb.IsEnabled = ChkInstallWeb.IsChecked == true;
    }

    private InstallConfig ReadConfig()
    {
        int.TryParse(TxtPort.Text.Trim(), out var port);
        int.TryParse(TxtWebPort.Text.Trim(), out var webPort);
        return new InstallConfig
        {
            ServerDb = TxtServer.Text.Trim(),
            UserDb = TxtUser.Text.Trim(),
            PasswordDb = TxtPassword.Password,
            ApsiDb = TxtApsiDb.Text.Trim(),
            PrenominaDb = TxtPrenominaDb.Text.Trim(),
            JwtKey = TxtJwt.Text.Trim(),
            Port = port,
            WebBase = TxtWebBase.Text.Trim(),
            TimeZone = TxtTimeZone.Text.Trim(),
            InstallPath = TxtInstallPath.Text.Trim(),
            ServiceName = TxtServiceName.Text.Trim(),
            ServiceDisplayName = "Prenomina API",

            InstallWeb = ChkInstallWeb.IsChecked == true,
            ApiUrl = TxtApiUrl.Text.Trim(),
            WebPort = webPort,
            WebInstallPath = TxtWebInstallPath.Text.Trim(),
            WebServiceName = TxtWebServiceName.Text.Trim(),
            WebServiceDisplayName = "Prenomina Web"
        };
    }

    private void OnGenerateJwt(object sender, RoutedEventArgs e)
    {
        TxtJwt.Text = InstallerService.GenerateJwtKey();
    }

    private async void OnTestConnection(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var cfg = ReadConfig();
        if (string.IsNullOrWhiteSpace(cfg.ServerDb) || string.IsNullOrWhiteSpace(cfg.UserDb))
        {
            SetStatus("Captura servidor, usuario y contraseña para probar la conexión.");
            return;
        }

        SetBusy(true, "Probando conexión...");
        var (ok, message) = await InstallerService.TestConnectionAsync(cfg, CancellationToken.None);
        AppendLog((ok ? "✔ " : "✗ ") + message);
        SetStatus(message);
        SetBusy(false);
    }

    private async void OnInstall(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        var cfg = ReadConfig();
        var error = cfg.Validate();
        if (error != null)
        {
            MessageBox.Show(error, "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var resumen = $"API:\n  {cfg.InstallPath}\n  Servicio: {cfg.ServiceName}  ·  Puerto: {cfg.Port}";
        if (cfg.InstallWeb)
            resumen += $"\n\nFrontend:\n  {cfg.WebInstallPath}\n  Servicio: {cfg.WebServiceName}  ·  Puerto: {cfg.WebPort}";

        var confirm = MessageBox.Show(
            $"Se instalará como servicio(s) de Windows:\n\n{resumen}\n\n¿Continuar?",
            "Confirmar instalación", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        SetBusy(true, "Instalando...");
        TxtLog.Clear();

        var progress = new Progress<string>(AppendLog);
        var installer = new InstallerService(progress);

        try
        {
            await Task.Run(() => installer.InstallAsync(cfg, CancellationToken.None));
            SetStatus("Instalación completada.");
            var msg = $"Instalación completada.\n\nAPI: http://localhost:{cfg.Port}\nSwagger: http://localhost:{cfg.Port}/swagger";
            if (cfg.InstallWeb)
                msg += $"\nFrontend: http://localhost:{cfg.WebPort}";
            MessageBox.Show(msg, "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog("");
            AppendLog("✗ ERROR: " + ex.Message);
            SetStatus("La instalación falló.");
            MessageBox.Show(ex.Message, "Error en la instalación", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // --- UI helpers ---

    private void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        BtnInstall.IsEnabled = !busy && InstallerService.PayloadExists;
        BtnTest.IsEnabled = !busy;
        if (status != null) SetStatus(status);
    }

    private void SetStatus(string text) => TxtStatus.Text = text;

    private void AppendLog(string line)
    {
        TxtLog.AppendText((TxtLog.Text.Length == 0 ? "" : Environment.NewLine) + line);
        LogScroll.ScrollToEnd();
    }
}
