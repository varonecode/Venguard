using System.Windows;
using System.Windows.Input;
using Venguard.Services;

namespace Venguard;

public partial class MainWindow : Window
{
    private readonly VencordRepairService _repairService;

    public MainWindow(VencordRepairService repairService)
    {
        InitializeComponent();
        _repairService = repairService;
    }

    public void UpdateDiscordStatus(DiscordStatus status)
    {
        StatusText.Text = status switch
        {
            { IsInstalled: false } => "Discord Stable not found",
            { IsVencordPatched: true } => "Vencord is patched",
            _ => "Discord Stable found — Vencord is not patched"
        };
    }

    public void RequestRepair()
    {
        RepairButton_Click(this, new RoutedEventArgs());
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

        try
        {
            if (_repairService.IsDiscordRunning())
            {
                MessageBox.Show(
                    "Discord is currently running. Please fully close Discord from the system tray before repairing Vencord.",
                    "Discord is running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                "Venguard will use the official Vencord installer to repair Discord. Continue?",
                "Repair Vencord",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            RepairButton.IsEnabled = false;
            RepairProgressBar.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = "Preparing repair...";
            StatusText.Text = "Repairing Vencord...";

            var progress = new Progress<string>(message =>
            {
                ProgressText.Text = message;
            });

            var repairResult = await _repairService.RepairAsync(
                progress);

            if (!repairResult.Success)
            {
                StatusText.Text = "Vencord repair failed";

                var details = string.IsNullOrWhiteSpace(
                    repairResult.Error)
                    ? repairResult.Output
                    : repairResult.Error;

                MessageBox.Show(
                    $"{repairResult.Message}\n\n{details}",
                    "Vencord Repair Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            StatusText.Text = "Vencord is patched";
            ProgressText.Text = "Repair completed successfully.";

            app.CompleteRepair(true);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Vencord repair failed";

            MessageBox.Show(
                ex.ToString(),
                "Venguard Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            app.CompleteRepair(false);

            RepairProgressBar.Visibility = Visibility.Collapsed;
            ProgressText.Visibility = Visibility.Collapsed;
            RepairButton.IsEnabled = true;
        }
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
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}