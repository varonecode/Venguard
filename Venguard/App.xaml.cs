using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Venguard.Config;
using Venguard.Services;

namespace Venguard;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private DiscordMonitor? _discordMonitor;
    private ConfigService? _configService;
    private VenguardConfig? _config;
    private StartupService _startupService = null!;
    private VencordRepairService _repairService = null!;
    private NotificationService _notificationService = null!;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _configService = new ConfigService();
        _config = _configService.Load();

        _startupService = new StartupService();
        _startupService.SetEnabled(_config.AutoStart);

        if (_config.IsFirstRun)
        {
            var wizard = new FirstRunWindow(_config);

            if (wizard.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            _config.IsFirstRun = false;
            _configService.Save(_config);
        }

        var discordService = new DiscordService();
        var installerDownloader = new VencordInstallerDownloader();
        var installerManager = new VencordInstallerManager(installerDownloader);
        var installerService = new VencordInstallerService();

        _repairService = new VencordRepairService(
            discordService,
            installerManager,
            installerService);

        _notificationService = new NotificationService();

        _mainWindow = new MainWindow(_repairService);

        var menu = new ContextMenu();

        var openItem = new MenuItem
        {
            Header = "Open Venguard"
        };

        openItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new MenuItem
        {
            Header = "Exit Venguard"
        };

        exitItem.Click += (_, _) => Shutdown();

        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Venguard",
            Icon = SystemIcons.Application,
            ContextMenu = menu
        };

        _discordMonitor = new DiscordMonitor(
            discordService,
            TimeSpan.FromSeconds(10));

        _discordMonitor.StatusChanged += DiscordMonitor_StatusChanged;

        _mainWindow.Show();
        _mainWindow.UpdateDiscordStatus(_discordMonitor.CurrentStatus);

        _discordMonitor.Start();

        _configService.Save(_config);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_discordMonitor is not null)
        {
            _discordMonitor.StatusChanged -= DiscordMonitor_StatusChanged;
            _discordMonitor.Dispose();
        }

        _trayIcon?.Dispose();

        base.OnExit(e);
    }

    private void DiscordMonitor_StatusChanged(
        object? sender,
        DiscordStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            _mainWindow?.UpdateDiscordStatus(status);

            if (status.IsInstalled && !status.IsVencordPatched)
            {
                _notificationService.ShowRepairNeeded();
            }
        });
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void App_DispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);

        MessageBox.Show(
            e.Exception.ToString(),
            "Venguard Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void App_UnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException("UnhandledException", exception);
        }
    }

    private static void LogException(string source, Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Venguard");

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "debug.log");

            File.AppendAllText(
                path,
                $"{DateTime.Now:O} {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}