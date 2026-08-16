using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Venguard.Config;
using Venguard.Services;

namespace Venguard;

public partial class MainWindow : Window
{
    private readonly VencordRepairService _repairService;
    private readonly VenguardConfig _config;
    private readonly ConfigService _configService;
    private readonly StartupService _startupService;
    private readonly DiscordService _discordService;
    private readonly VencordInstallerManager _installerManager;
    private readonly OpenAsarService _openAsarService;

    private CancellationTokenSource? _repairCancellation;
    private bool _forceClose;

    public MainWindow(
        VencordRepairService repairService,
        VenguardConfig config,
        ConfigService configService,
        StartupService startupService,
        DiscordService discordService,
        VencordInstallerManager installerManager,
        OpenAsarService openAsarService)
    {
        _repairService =
            repairService;

        _config =
            config;

        _configService =
            configService;

        _startupService =
            startupService;

        _discordService =
            discordService;

        _installerManager =
            installerManager;

        _openAsarService =
            openAsarService;

        InitializeComponent();

        LoadSettings();

        ShowDashboardView();
    }

    public void UpdateDiscordStatus(
        DiscordStatus status)
    {
        if (!status.IsInstalled)
        {
            StatusText.Text =
                "Discord not found";

            OpenAsarStatusText.Text =
                "Unavailable";

            return;
        }

        StatusText.Text =
            status.IsVencordPatched
                ? "Protected"
                : "Needs repair";

        OpenAsarStatusText.Text =
            status.IsOpenAsar
                ? "Enabled"
                : "Disabled";
    }

    public void RequestRepair()
    {
        ShowDashboardView();

        RepairButton_Click(
            this,
            new RoutedEventArgs());
    }

    public void ShowSettingsView()
    {
        LoadSettings();

        ShowSettingsViewInternal();

        Activate();
    }

    public void ForceClose()
    {
        _forceClose = true;

        Close();
    }

    private void ShowDashboardView()
    {
        SettingsView.Visibility =
            Visibility.Collapsed;

        DashboardView.Visibility =
            Visibility.Visible;

        DashboardTranslate.X = -12;

        DashboardView.Opacity = 0;

        SetNavigationState(
            DashboardNavButton,
            true);

        SetNavigationState(
            SettingsNavButton,
            false);

        DashboardView.BeginStoryboard(
            (Storyboard)FindResource(
                "DashboardEnter"));
    }

    private void ShowSettingsViewInternal()
    {
        DashboardView.Visibility =
            Visibility.Collapsed;

        SettingsView.Visibility =
            Visibility.Visible;

        SettingsTranslate.X =
            12;

        SettingsView.Opacity =
            0;

        SetNavigationState(
            DashboardNavButton,
            false);

        SetNavigationState(
            SettingsNavButton,
            true);

        SettingsView.BeginStoryboard(
            (Storyboard)FindResource(
                "SettingsEnter"));
    }

    private static void SetNavigationState(
        Button button,
        bool selected)
    {
        button.Background =
            selected
                ? Application.Current.Resources[
                    "SurfaceSelectedBrush"]
                    as System.Windows.Media.Brush
                : System.Windows.Media.Brushes.Transparent;

        button.Foreground =
            selected
                ? Application.Current.Resources[
                    "PurpleBrightBrush"]
                    as System.Windows.Media.Brush
                : Application.Current.Resources[
                    "MutedForegroundBrush"]
                    as System.Windows.Media.Brush;
    }

    private void LoadSettings()
    {
        AutoStartCheckBox.IsChecked =
            _config.AutoStart;

        StartMinimizedCheckBox.IsChecked =
            _config.StartMinimized;

        MinimizeToTrayCheckBox.IsChecked =
            _config.MinimizeToTray;

        CloseToTrayCheckBox.IsChecked =
            _config.CloseToTray;

        ConfirmBeforeRepairCheckBox.IsChecked =
            _config.ConfirmBeforeRepair;

        LaunchDiscordCheckBox.IsChecked =
            _config.LaunchDiscordAfterPatch;

        OpenAsarCheckBox.IsChecked =
            _config.UseOpenAsar;

        NotificationsCheckBox.IsChecked =
            _config.EnableNotifications;

        NotifySuccessCheckBox.IsChecked =
            _config.NotifyOnRepairSuccess;

        NotifyFailureCheckBox.IsChecked =
            _config.NotifyOnRepairFailure;

        MonitorIntervalComboBox.SelectedValue =
            _config.MonitorIntervalSeconds
                .ToString();

        TrayStateText.Text =
            _config.CloseToTray
                ? "Close button → system tray"
                : "Close button → exit app";

        MonitorIntervalText.Text =
            $"Monitoring every {_config.MonitorIntervalSeconds} seconds";

        SettingsResultText.Text =
            string.Empty;
    }

    private async void SaveSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var newAutoStart =
                AutoStartCheckBox.IsChecked == true;

            var newStartMinimized =
                StartMinimizedCheckBox.IsChecked == true;

            var newMinimizeToTray =
                MinimizeToTrayCheckBox.IsChecked == true;

            var newCloseToTray =
                CloseToTrayCheckBox.IsChecked == true;

            var newConfirmBeforeRepair =
                ConfirmBeforeRepairCheckBox.IsChecked == true;

            var newLaunchDiscord =
                LaunchDiscordCheckBox.IsChecked == true;

            var newUseOpenAsar =
                OpenAsarCheckBox.IsChecked == true;

            var newNotifications =
                NotificationsCheckBox.IsChecked == true;

            var newNotifySuccess =
                NotifySuccessCheckBox.IsChecked == true;

            var newNotifyFailure =
                NotifyFailureCheckBox.IsChecked == true;

            var newInterval =
                GetSelectedMonitorInterval();

            if (newUseOpenAsar !=
                _config.UseOpenAsar)
            {
                var installation =
                    _discordService
                        .GetInstallation();

                if (installation is null)
                {
                    SettingsResultText.Text =
                        "Discord Stable was not found.";

                    return;
                }

                if (_discordService
                    .IsDiscordRunning())
                {
                    SettingsResultText.Text =
                        "Close Discord before changing OpenAsar.";

                    return;
                }

                var confirmation =
                    MessageBox.Show(
                        newUseOpenAsar
                            ? "Install OpenAsar using the official Vencord installer?"
                            : "Remove OpenAsar using the official Vencord installer?",
                        newUseOpenAsar
                            ? "Install OpenAsar"
                            : "Remove OpenAsar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (confirmation !=
                    MessageBoxResult.Yes)
                {
                    return;
                }

                SettingsResultText.Text =
                    newUseOpenAsar
                        ? "Installing OpenAsar..."
                        : "Removing OpenAsar...";

                var installerPath =
                    await _installerManager
                        .GetInstallerAsync();

                var openAsarResult =
                    await _openAsarService
                        .SetEnabledAsync(
                            installerPath,
                            installation.DiscordPath,
                            newUseOpenAsar);

                if (!openAsarResult.Success)
                {
                    SettingsResultText.Text =
                        openAsarResult.Message;

                    return;
                }
            }

            _config.AutoStart =
                newAutoStart;

            _config.StartMinimized =
                newStartMinimized;

            _config.MinimizeToTray =
                newMinimizeToTray;

            _config.CloseToTray =
                newCloseToTray;

            _config.ConfirmBeforeRepair =
                newConfirmBeforeRepair;

            _config.LaunchDiscordAfterPatch =
                newLaunchDiscord;

            _config.UseOpenAsar =
                newUseOpenAsar;

            _config.EnableNotifications =
                newNotifications;

            _config.NotifyOnRepairSuccess =
                newNotifySuccess;

            _config.NotifyOnRepairFailure =
                newNotifyFailure;

            _config.MonitorIntervalSeconds =
                newInterval;

            _configService.Save(
                _config);

            _startupService.SetEnabled(
                _config.AutoStart);

            UpdateDiscordStatus(
                _discordService.GetStatus());

            TrayStateText.Text =
                _config.CloseToTray
                    ? "Close button → system tray"
                    : "Close button → exit app";

            MonitorIntervalText.Text =
                $"Monitoring every {_config.MonitorIntervalSeconds} seconds";

            SettingsResultText.Text =
                "Settings saved.";
        }
        catch (Exception ex)
        {
            SettingsResultText.Text =
                "Could not save settings.";

            MessageBox.Show(
                ex.ToString(),
                "Venguard Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadSettings();

        SettingsResultText.Text =
            "Changes discarded.";
    }

    private void OpenDataFolderButton_Click(
        object sender,
        RoutedEventArgs e)
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

    private void ClearLogsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var directory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "Venguard");

            foreach (var path in new[]
                     {
                         Path.Combine(
                             directory,
                             "debug.log"),

                         Path.Combine(
                             directory,
                             "toast-debug.log")
                     })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                }
            }

            SettingsResultText.Text =
                "Debug logs cleared.";
        }
        catch (Exception ex)
        {
            SettingsResultText.Text =
                "Could not clear debug logs.";

            MessageBox.Show(
                ex.ToString(),
                "Venguard",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ResetSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var result =
            MessageBox.Show(
                "Reset all Venguard settings to their defaults?",
                "Reset settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result !=
            MessageBoxResult.Yes)
        {
            return;
        }

        _config.IsFirstRun = false;
        _config.AutoStart = true;
        _config.StartMinimized = false;
        _config.MinimizeToTray = true;
        _config.CloseToTray = true;
        _config.LaunchDiscordAfterPatch = true;
        _config.UseOpenAsar = true;
        _config.ConfirmBeforeRepair = true;
        _config.EnableNotifications = true;
        _config.NotifyOnRepairSuccess = true;
        _config.NotifyOnRepairFailure = true;
        _config.MonitorIntervalSeconds = 10;

        _configService.Save(
            _config);

        _startupService.SetEnabled(
            _config.AutoStart);

        LoadSettings();

        SettingsResultText.Text =
            "Settings reset.";
    }

    private void UninstallButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var result =
            MessageBox.Show(
                "This removes Venguard's startup registration and user data. Discord itself will not be modified.\n\nContinue?",
                "Uninstall Venguard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result !=
            MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _startupService.SetEnabled(
                false);

            var directory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "Venguard");

            if (Directory.Exists(directory))
            {
                foreach (var file in
                         Directory.GetFiles(
                             directory,
                             "*",
                             SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }

            _forceClose = true;

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Uninstall Venguard",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private int GetSelectedMonitorInterval()
    {
        if (MonitorIntervalComboBox.SelectedItem
            is ComboBoxItem item &&
            int.TryParse(
                item.Tag?.ToString(),
                out var seconds))
        {
            return seconds;
        }

        return 10;
    }

    private void DashboardNavButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowDashboardView();
    }

    private void SettingsNavButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadSettings();

        ShowSettingsViewInternal();
    }

    private async void RepairButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current is not App app)
        {
            return;
        }

        if (!app.TryBeginRepair())
        {
            return;
        }

        if (_repairCancellation is not null)
        {
            app.CompleteRepair(false);
            return;
        }

        try
        {
            ResultText.Visibility =
                Visibility.Collapsed;

            if (_repairService.IsDiscordRunning())
            {
                ResultText.Text =
                    "Close Discord before starting a repair.";

                ResultText.Visibility =
                    Visibility.Visible;

                MessageBox.Show(
                    "Discord is currently running. Please fully close Discord from the system tray before repairing Vencord.",
                    "Discord is running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (_config.ConfirmBeforeRepair)
            {
                var confirmation =
                    MessageBox.Show(
                        "Venguard will use the official Vencord installer to repair Discord.",
                        "Repair Vencord",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (confirmation !=
                    MessageBoxResult.Yes)
                {
                    return;
                }
            }

            _repairCancellation =
                new CancellationTokenSource();

            RepairButton.IsEnabled =
                false;

            CancelRepairButton.Visibility =
                Visibility.Visible;

            CancelRepairButton.IsEnabled =
                true;

            RepairProgressBar.Visibility =
                Visibility.Visible;

            ProgressText.Visibility =
                Visibility.Visible;

            ResultText.Visibility =
                Visibility.Collapsed;

            ProgressText.Text =
                "Preparing repair...";

            StatusText.Text =
                "Repairing...";

            var cancellationToken =
                _repairCancellation.Token;

            var progress =
                new Progress<string>(
                    message =>
                    {
                        if (!cancellationToken
                            .IsCancellationRequested)
                        {
                            ProgressText.Text =
                                message;
                        }
                    });

            var repairResult =
                await _repairService
                    .RepairAsync(
                        _config.UseOpenAsar,
                        progress,
                        cancellationToken);

            if (cancellationToken
                .IsCancellationRequested)
            {
                StatusText.Text =
                    "Repair cancelled";

                ProgressText.Text =
                    "Repair was cancelled.";

                ResultText.Visibility =
                    Visibility.Collapsed;

                return;
            }

            if (repairResult.Stage ==
                VencordRepairStage.OpenAsar &&
                repairResult.VencordSucceeded)
            {
                StatusText.Text =
                    "Protected";

                OpenAsarStatusText.Text =
                    "Change failed";

                ProgressText.Text =
                    "Vencord was repaired, but OpenAsar could not be changed.";

                ResultText.Text =
                    repairResult.Message;

                ResultText.Visibility =
                    Visibility.Visible;

                if (_config.NotifyOnRepairFailure)
                {
                    MessageBox.Show(
                        repairResult.Message,
                        "Repair Partially Completed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                return;
            }

            if (!repairResult.Success)
            {
                StatusText.Text =
                    "Needs repair";

                var details =
                    string.IsNullOrWhiteSpace(
                        repairResult.Error)
                        ? repairResult.Output
                        : repairResult.Error;

                ProgressText.Text =
                    "Repair failed.";

                ResultText.Text =
                    repairResult.Message;

                ResultText.Visibility =
                    Visibility.Visible;

                if (_config.NotifyOnRepairFailure)
                {
                    MessageBox.Show(
                        $"{repairResult.Message}\n\n{details}",
                        "Venguard Repair Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return;
            }

            UpdateDiscordStatus(
                _discordService.GetStatus());

            ProgressText.Text =
                "Repair complete.";

            ResultText.Text =
                "Vencord was successfully repaired.";

            ResultText.Visibility =
                Visibility.Visible;

            app.CompleteRepair(
                true);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text =
                "Repair cancelled";

            ProgressText.Text =
                "Repair was cancelled.";

            ResultText.Visibility =
                Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            StatusText.Text =
                "Repair failed";

            ProgressText.Text =
                "An unexpected error occurred.";

            ResultText.Text =
                ex.Message;

            ResultText.Visibility =
                Visibility.Visible;

            MessageBox.Show(
                ex.ToString(),
                "Venguard Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _repairCancellation?.Dispose();

            _repairCancellation =
                null;

            app.CompleteRepair(false);

            RepairProgressBar.Visibility =
                Visibility.Collapsed;

            ProgressText.Visibility =
                Visibility.Visible;

            CancelRepairButton.Visibility =
                Visibility.Collapsed;

            CancelRepairButton.IsEnabled =
                true;

            RepairButton.IsEnabled =
                true;
        }
    }

    private void CancelRepairButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_repairCancellation is null)
        {
            return;
        }

        _repairCancellation.Cancel();

        CancelRepairButton.IsEnabled =
            false;

        ProgressText.Text =
            "Cancelling repair...";
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();

            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_config.MinimizeToTray)
        {
            Hide();
        }
        else
        {
            WindowState =
                WindowState.Minimized;
        }
    }

    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_forceClose ||
            !_config.CloseToTray)
        {
            Application.Current.Shutdown();

            return;
        }

        Hide();
    }

    protected override void OnClosing(
        System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose &&
            _config.CloseToTray)
        {
            e.Cancel = true;

            Hide();
        }

        base.OnClosing(e);
    }

    private void ToggleMaximize()
    {
        WindowState =
            WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }
}