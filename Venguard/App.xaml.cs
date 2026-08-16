using System.Drawing;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _configService = new ConfigService();
        _config = _configService.Load();
        if (_config.IsFirstRun)
        {
            _config.IsFirstRun = false;
            _configService.Save(_config);
        }

         _mainWindow = new MainWindow();

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

        var discordService = new DiscordService();

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
}