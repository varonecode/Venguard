using System.Diagnostics;
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
    private Icon? _trayIconImage;
    private MainWindow? _mainWindow;

    private DiscordMonitor? _discordMonitor;

    private ConfigService? _configService;
    private VenguardConfig? _config;

    private StartupService _startupService = null!;
    private DiscordLauncherService _discordLauncher = null!;
    private VencordRepairService _repairService = null!;
    private NotificationService _notificationService = null!;
    private DiscordService _discordService = null!;
    private VencordInstallerManager _installerManager = null!;
    private OpenAsarService _openAsarService = null!;

    private bool _repairInProgress;

    public App()
    {
        DispatcherUnhandledException +=
            App_DispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException +=
            App_UnhandledException;
    }

    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        _configService =
            new ConfigService();

        _config =
            _configService.Load();

        _startupService =
            new StartupService();

        _startupService.SetEnabled(
            _config.AutoStart);

        _discordLauncher =
            new DiscordLauncherService();

        if (_config.IsFirstRun)
        {
            var wizard =
                new FirstRunWindow(_config);

            if (wizard.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            _config.IsFirstRun = false;

            _configService.Save(
                _config);

            _startupService.SetEnabled(
                _config.AutoStart);
        }

        _discordService =
            new DiscordService();

        var installerDownloader =
            new VencordInstallerDownloader();

        _installerManager =
            new VencordInstallerManager(
                installerDownloader);

        var installerService =
            new VencordInstallerService();

        _openAsarService =
            new OpenAsarService();

        _repairService =
            new VencordRepairService(
                _discordService,
                _installerManager,
                installerService,
                _openAsarService);

        _notificationService =
            new NotificationService();

        _notificationService.Activated +=
            Notification_Activated;

        _mainWindow =
            new MainWindow(
                _repairService,
                _config,
                _configService,
                _startupService,
                _discordService,
                _installerManager,
                _openAsarService);

        CreateTrayIcon();

        var interval =
            TimeSpan.FromSeconds(
                Math.Max(
                    5,
                    _config.MonitorIntervalSeconds));

        _discordMonitor =
            new DiscordMonitor(
                _discordService,
                interval);

        _discordMonitor.StatusChanged +=
            DiscordMonitor_StatusChanged;

        _mainWindow.UpdateDiscordStatus(
            _discordMonitor.CurrentStatus);

        ApplyMonitorSettings();

        _configService.Save(
            _config);

        if (_config.StartMinimized)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    protected override void OnExit(
        ExitEventArgs e)
    {
        if (_discordMonitor is not null)
        {
            _discordMonitor.StatusChanged -=
                DiscordMonitor_StatusChanged;

            _discordMonitor.Dispose();
        }

        _notificationService.Activated -=
            Notification_Activated;

        if (_trayIcon is not null)
        {
            _trayIcon.Visibility =
                Visibility.Hidden;

            _trayIcon.Dispose();

            _trayIcon = null;
        }

        _trayIconImage?.Dispose();
        _trayIconImage = null;

        base.OnExit(e);
    }

    public void ApplyMonitorSettings()
    {
        if (_discordMonitor is null ||
            _config is null)
        {
            return;
        }

        var interval =
            TimeSpan.FromSeconds(
                Math.Max(
                    5,
                    _config.MonitorIntervalSeconds));

        _discordMonitor.UpdateInterval(
            interval);

        if (_config.EnableBackgroundMonitoring)
        {
            _discordMonitor.Start();
        }
        else
        {
            _discordMonitor.Stop();
        }
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

    public void CompleteRepair(
        bool success)
    {
        _repairInProgress = false;

        if (!success)
        {
            return;
        }

        var currentStatus =
            _discordService.GetStatus();

        _mainWindow?.UpdateDiscordStatus(
            currentStatus);

        if (currentStatus.IsInstalled &&
            currentStatus.IsVencordPatched)
        {
            _notificationService
                .ResetRepairNotificationCooldown();
        }

        if (_config?.LaunchDiscordAfterPatch ==
            true)
        {
            _discordLauncher.Launch();
        }
    }

    private void CreateTrayIcon()
    {
        var icoPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Venguard.ico");

        if (!File.Exists(icoPath))
        {
            var projectAssetPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "Assets",
                    "Venguard.ico");

            if (File.Exists(projectAssetPath))
            {
                icoPath =
                    Path.GetFullPath(
                        projectAssetPath);
            }
        }

        if (File.Exists(icoPath))
        {
            using var sourceIcon =
                new Icon(icoPath);

            _trayIconImage =
                (Icon)sourceIcon.Clone();
        }
        else
        {
            _trayIconImage =
                (Icon)SystemIcons.Application.Clone();
        }

        var menu =
            new ContextMenu();

        var header =
            new MenuItem
            {
                Header =
                    _config?.UseOpenAsar == true
                        ? "Venguard  •  OpenAsar on"
                        : "Venguard  •  OpenAsar off",

                IsEnabled = false,

                FontWeight =
                    System.Windows.FontWeights.SemiBold
            };

        menu.Items.Add(
            header);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(
            CreateTrayItem(
                "⌂",
                "Open Venguard",
                ShowMainWindow));

        menu.Items.Add(
            CreateTrayItem(
                "⚙",
                "Settings",
                ShowSettingsWindow));

        menu.Items.Add(
            CreateTrayItem(
                "↻",
                "Check Discord now",
                CheckDiscordNow));

        menu.Items.Add(
            CreateTrayItem(
                "⚡",
                "Repair Vencord",
                StartRepairFromTray));

        menu.Items.Add(
            CreateTrayItem(
                "●",
                "Open Discord",
                OpenDiscord));

        menu.Items.Add(
            CreateTrayItem(
                "▣",
                "Open Venguard data",
                OpenDataFolder));

        menu.Items.Add(
            new Separator());

        menu.Items.Add(
            CreateTrayItem(
                "×",
                "Exit Venguard",
                Shutdown));

        _trayIcon =
            new TaskbarIcon
            {
                ToolTipText =
                    "Venguard — Discord protection utility",

                Icon =
                    _trayIconImage,

                ContextMenu =
                    menu,

                Visibility =
                    Visibility.Visible
            };
    }

    private static MenuItem CreateTrayItem(
        string icon,
        string header,
        Action action)
    {
        var item =
            new MenuItem
            {
                Header = header
            };

        item.Icon =
            new TextBlock
            {
                Text = icon,

                FontFamily =
                    new System.Windows.Media.FontFamily(
                        "Segoe UI Symbol"),

                FontSize = 14,

                Foreground =
                    Application.Current.Resources[
                        "PurpleBrightBrush"]
                    as System.Windows.Media.Brush
            };

        item.Click +=
            (_, _) => action();

        return item;
    }

    private void CheckDiscordNow()
    {
        _discordMonitor?.CheckNow();
    }

    private void StartRepairFromTray()
    {
        if (_mainWindow is null)
        {
            return;
        }

        ShowMainWindow();

        _mainWindow.RequestRepair();
    }

    private void OpenDiscord()
    {
        _discordLauncher.Launch();
    }

    private void OpenDataFolder()
    {
        var directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Venguard");

        Directory.CreateDirectory(
            directory);

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    "explorer.exe",

                Arguments =
                    $"\"{directory}\"",

                UseShellExecute = true
            });
    }

    private void Notification_Activated(
        object? sender,
        string arguments)
    {
        var normalized =
            arguments;

        for (var i = 0; i < 3; i++)
        {
            var decoded =
                Uri.UnescapeDataString(
                    normalized);

            if (decoded ==
                normalized)
            {
                break;
            }

            normalized =
                decoded;
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

                StartRepairFromTray();
            }));
    }

    private void DiscordMonitor_StatusChanged(
        object? sender,
        DiscordStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            _mainWindow?.UpdateDiscordStatus(
                status);

            if (status.IsInstalled &&
                status.IsVencordPatched)
            {
                _notificationService
                    .ResetRepairNotificationCooldown();
            }

            if (_repairInProgress)
            {
                return;
            }

            if (status.IsInstalled &&
                !status.IsVencordPatched &&
                _config?.EnableNotifications == true)
            {
                _notificationService
                    .ShowRepairNeeded();
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
        _mainWindow?.ShowSettingsView();
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
        if (e.ExceptionObject
            is Exception exception)
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
            var directory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "Venguard");

            Directory.CreateDirectory(
                directory);

            File.AppendAllText(
                Path.Combine(
                    directory,
                    "debug.log"),
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