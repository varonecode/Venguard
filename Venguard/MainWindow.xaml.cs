using System.Windows;
using System.Windows.Input;
using Venguard.Services;

namespace Venguard;

public partial class MainWindow : Window
{
    private readonly VencordRepairService _repairService;
    private CancellationTokenSource? _repairCancellation;

    public MainWindow(
        VencordRepairService repairService)
    {
        InitializeComponent();
        _repairService = repairService;
    }

    public void UpdateDiscordStatus(
        DiscordStatus status)
    {
        if (!status.IsInstalled)
        {
            StatusText.Text =
                "Discord Stable not found";

            OpenAsarStatusText.Text =
                "OpenAsar: —";

            return;
        }

        StatusText.Text =
            status.IsVencordPatched
                ? "Vencord: Patched"
                : "Vencord: Not patched";

        OpenAsarStatusText.Text =
            status.IsOpenAsar
                ? "OpenAsar: Enabled"
                : "OpenAsar: Disabled";
    }

    public void RequestRepair()
    {
        RepairButton_Click(
            this,
            new RoutedEventArgs());
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

            var confirmation = MessageBox.Show(
                "Venguard will use the official Vencord installer to repair Discord.",
                "Repair Vencord",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _repairCancellation =
                new CancellationTokenSource();

            RepairButton.IsEnabled = false;

            CancelRepairButton.Visibility =
                Visibility.Visible;

            CancelRepairButton.IsEnabled = true;

            RepairProgressBar.Visibility =
                Visibility.Visible;

            ProgressText.Visibility =
                Visibility.Visible;

            ProgressText.Text =
                "Preparing repair...";

            StatusText.Text =
                "Repairing Vencord...";

            var useOpenAsar =
                ReadUseOpenAsarSetting();

            var cancellationToken =
                _repairCancellation.Token;

            var progress =
                new Progress<string>(
                    message =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ProgressText.Text = message;
                        }
                    });

            var repairResult =
                await _repairService.RepairAsync(
                    useOpenAsar,
                    progress,
                    cancellationToken);

            // Cancellation always wins over a near-simultaneous
            // success result from the installer.
            if (cancellationToken.IsCancellationRequested)
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
                    "Vencord: Patched";

                OpenAsarStatusText.Text =
                    "OpenAsar: Change failed";

                ResultText.Text =
                    "Vencord was repaired successfully, but the OpenAsar setting could not be applied.";

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
                    "Vencord repair failed";

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

            StatusText.Text =
                "Vencord: Patched";

            OpenAsarStatusText.Text =
                useOpenAsar
                    ? "OpenAsar: Enabled"
                    : "OpenAsar: Disabled";

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
                "Vencord repair failed";

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
                Visibility.Collapsed;

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

    private static bool ReadUseOpenAsarSetting()
    {
        var configService =
            new Config.ConfigService();

        var config =
            configService.Load();

        return config.UseOpenAsar;
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