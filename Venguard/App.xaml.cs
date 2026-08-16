using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Venguard.Config;
using Venguard.Services;

namespace Venguard;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private DiscordMonitor? _discordMonitor;
    private ConfigService? _configService;
    private VenguardConfig? _config;
    private StartupService _startupService = null!;
    private DiscordLauncherService _discordLauncher = null!;
    private VencordRepairService _repairService = null!;
    private NotificationService _notificationService = null!;

    private bool _repairInProgress;

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

        _discordLauncher = new DiscordLauncherService();

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

            _startupService.SetEnabled(_config.AutoStart);
        }

        var discordService = new DiscordService();
        var installerDownloader = new VencordInstallerDownloader();
        var installerManager =
            new VencordInstallerManager(installerDownloader);
        var installerService = new VencordInstallerService();

        _repairService = new VencordRepairService(
            discordService,
            installerManager,
            installerService);

        _notificationService = new NotificationService();
        _notificationService.Activated += Notification_Activated;

        _mainWindow = new MainWindow(_repairService);

        var menu = new ContextMenu();

        var openItem = new MenuItem
        {
            Header = "Open Venguard"
        };

        openItem.Click += (_, _) => ShowMainWindow();

        var settingsItem = new MenuItem
        {
            Header = "Settings"
        };

        settingsItem.Click += (_, _) => ShowSettingsWindow();

        var exitItem = new MenuItem
        {
            Header = "Exit Venguard"
        };

        exitItem.Click += (_, _) => Shutdown();

        menu.Items.Add(openItem);
        menu.Items.Add(settingsItem);
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

        _discordMonitor.StatusChanged +=
            DiscordMonitor_StatusChanged;

        _mainWindow.Show();
        _mainWindow.UpdateDiscordStatus(
            _discordMonitor.CurrentStatus);

        _discordMonitor.Start();

        _configService.Save(_config);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_discordMonitor is not null)
        {
            _discordMonitor.StatusChanged -=
                DiscordMonitor_StatusChanged;

            _discordMonitor.Dispose();
        }

        _notificationService.Activated -= Notification_Activated;

        _trayIcon?.Dispose();

        base.OnExit(e);
    }

    public bool TryBeginRepair()
    {
        if (_repairInProgress)
        {
            return false;
        }

        _repairInProgress = true;
        return true;
    }

    public void CompleteRepair(bool success)
    {
        _repairInProgress = false;

        if (!success)
        {
            return;
        }

        _mainWindow?.UpdateDiscordStatus(
            new DiscordStatus(
                true,
                true,
                null));

        if (_config?.LaunchDiscordAfterPatch == true)
        {
            _discordLauncher.Launch();
        }
    }

    private void Notification_Activated(
        object? sender,
        string arguments)
    {
        var normalized = arguments;

        for (var i = 0; i < 3; i++)
        {
            var decoded =
                Uri.UnescapeDataString(normalized);

            if (decoded == normalized)
            {
                break;
            }

            normalized = decoded;
        }

        if (!normalized.Contains(
                "repair",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                if (_repairInProgress)
                {
                    return;
                }

                ShowMainWindow();
                _mainWindow?.RequestRepair();
            }));
    }

    private void DiscordMonitor_StatusChanged(
        object? sender,
        DiscordStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            _mainWindow?.UpdateDiscordStatus(status);

            if (_repairInProgress)
            {
                return;
            }

            if (status.IsInstalled &&
                !status.IsVencordPatched)
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

        if (_mainWindow.WindowState ==
            WindowState.Minimized)
        {
            _mainWindow.WindowState =
                WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void ShowSettingsWindow()
    {
        if (_config is null)
        {
            return;
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_config)
        {
            Owner = _mainWindow
        };

        try
        {
            var result = _settingsWindow.ShowDialog();

            if (result == true)
            {
                _configService!.Save(_config);
                _startupService.SetEnabled(
                    _config.AutoStart);
            }
        }
        finally
        {
            _settingsWindow = null;
        }
    }

    private void App_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(
            "DispatcherUnhandledException",
            e.Exception);

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
            LogException(
                "UnhandledException",
                exception);
        }
    }

    private static void LogException(
        string source,
        Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Venguard");

            Directory.CreateDirectory(directory);

            File.AppendAllText(
                Path.Combine(directory, "debug.log"),
                $"{DateTime.Now:O}{Environment.NewLine}" +
                $"{source}{Environment.NewLine}" +
                $"{exception}{Environment.NewLine}" +
                $"{Environment.NewLine}");
        }
        catch
        {
        }
    }
}