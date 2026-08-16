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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private async void RepairButton_Click(object sender, RoutedEventArgs e)
    {
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
            StatusText.Text = "Repairing Vencord...";

            var repairResult = await _repairService.RepairAsync();

            StatusText.Text = repairResult.Success
                ? "Vencord is patched"
                : "Vencord repair failed";

            if (!repairResult.Success)
            {
                var details = string.IsNullOrWhiteSpace(repairResult.Error)
                    ? repairResult.Output
                    : repairResult.Error;

                MessageBox.Show(
                    $"{repairResult.Message}\n\n{details}",
                    "Vencord Repair Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Venguard",
                "debug.log");

            try
            {
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(path)!);

                System.IO.File.AppendAllText(
                    path,
                    $"{DateTime.Now:O} RepairButton_Click{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }

            MessageBox.Show(
                ex.ToString(),
                "Venguard Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            StatusText.Text = "Vencord repair failed";
        }
        finally
        {
            RepairButton.IsEnabled = true;
        }
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}