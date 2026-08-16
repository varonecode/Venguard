using System.Windows;
using System.Windows.Input;
using Venguard.Services;

namespace Venguard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}