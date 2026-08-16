using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace Venguard;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        _mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
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