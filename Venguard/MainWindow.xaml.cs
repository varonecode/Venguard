using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public MainWindow(
        VencordRepairService repairService,
        VenguardConfig config,
        ConfigService configService,
        StartupService startupService,
        DiscordService discordService,
        VencordInstallerManager installerManager,
        OpenAsarService openAsarService)
    {
        _repairService = repairService;
        _config = config;
        _configService = configService;
        _startupService = startupService;
        _discordService = discordService;
        _installerManager = installerManager;
        _openAsarService = openAsarService;

        InitializeComponent();

        LoadSettings();
        ShowDashboardView();
    }

    public void UpdateDiscordStatus(
        DiscordStatus status)
    {
        if (!status.IsInstalled)
        {
            StatusText.Text = "Discord not found";
            OpenAsarStatusText.Text = "Unavailable";
            return;
        }

        StatusText.Text =
            status.IsVencordPatched
                ? "Patched"
                : "Not patched";

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

    private void ShowDashboardView()
    {
        DashboardView.Visibility =
            Visibility.Visible;

        SettingsView.Visibility =
            Visibility.Collapsed;

        SetNavigationState(
            DashboardNavButton,
            true);

        SetNavigationState(
            SettingsNavButton,
            false);
    }

    private void ShowSettingsViewInternal()
    {
        DashboardView.Visibility =
            Visibility.Collapsed;

        SettingsView.Visibility =
            Visibility.Visible;

        SetNavigationState(
            DashboardNavButton,
            false);

        SetNavigationState(
            SettingsNavButton,
            true);
    }

    private static void SetNavigationState(
        Button button,
        bool selected)
    {
        button.Background =
            selected
                ? Application.Current.Resources[
                    "SurfaceElevatedBrush"]
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

        LaunchDiscordCheckBox.IsChecked =
            _config.LaunchDiscordAfterPatch;

        OpenAsarCheckBox.IsChecked =
            _config.UseOpenAsar;

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

            var newLaunchDiscord =
                LaunchDiscordCheckBox.IsChecked == true;

            var newUseOpenAsar =
                OpenAsarCheckBox.IsChecked == true;

            if (newUseOpenAsar !=
                _config.UseOpenAsar)
            {
                var installation =
                    _discordService.GetInstallation();

                if (installation is null)
                {
                    SettingsResultText.Text =
                        "Discord Stable was not found.";

                    return;
                }

                if (_discordService.IsDiscordRunning())
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

                SettingsResultText.Text =
                    newUseOpenAsar
                        ? "OpenAsar enabled."
                        : "OpenAsar disabled.";
            }

            _config.AutoStart =
                newAutoStart;

            _config.LaunchDiscordAfterPatch =
                newLaunchDiscord;

            _config.UseOpenAsar =
                newUseOpenAsar;

            _configService.Save(
                _config);

            _startupService.SetEnabled(
                _config.AutoStart);

            UpdateDiscordStatus(
                _discordService.GetStatus());

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

            ProgressText.Text =
                "Preparing repair...";

            StatusText.Text =
                "Repairing...";

            var useOpenAsar =
                _config.UseOpenAsar;

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
                        useOpenAsar,
                        progress,
                        cancellationToken);

            if (cancellationToken
                .IsCancellationRequested)
            {
                StatusText.Text =
                    "Repair cancelled";

                ResultText.Text =
                    "Repair was cancelled.";

                ResultText.Visibility =
                    Visibility.Visible;

                return;
            }

            if (repairResult.Stage ==
                VencordRepairStage.OpenAsar &&
                repairResult.VencordSucceeded)
            {
                StatusText.Text =
                    "Patched";

                OpenAsarStatusText.Text =
                    "Change failed";

                ResultText.Text =
                    "Vencord was repaired, but the OpenAsar setting could not be applied.";

                ResultText.Visibility =
                    Visibility.Visible;

                MessageBox.Show(
                    repairResult.Message,
                    "Repair Partially Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!repairResult.Success)
            {
                StatusText.Text =
                    "Repair failed";

                ResultText.Text =
                    repairResult.Message;

                ResultText.Visibility =
                    Visibility.Visible;

                var details =
                    string.IsNullOrWhiteSpace(
                        repairResult.Error)
                        ? repairResult.Output
                        : repairResult.Error;

                MessageBox.Show(
                    $"{repairResult.Message}\n\n{details}",
                    "Venguard Repair Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            UpdateDiscordStatus(
                _discordService.GetStatus());

            ProgressText.Text =
                "Repair completed successfully.";

            ResultText.Text =
                "Repair completed successfully.";

            ResultText.Visibility =
                Visibility.Visible;

            app.CompleteRepair(true);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text =
                "Repair cancelled";

            ResultText.Text =
                "Repair was cancelled.";

            ResultText.Visibility =
                Visibility.Visible;
        }
        catch (Exception ex)
        {
            StatusText.Text =
                "Repair failed";

            ResultText.Text =
                "Repair failed.";

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
            _repairCancellation = null;

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
        Hide();
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
        Hide();
    }

    private void ToggleMaximize()
    {
        WindowState =
            WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }
}