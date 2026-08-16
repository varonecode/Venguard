using WindowsToastNotifyApi;

namespace Venguard.Services;

public sealed class NotificationService
{
    public event EventHandler<string>? Activated;

    public NotificationService()
    {
        Toast.Initialize(
            appId: "Venguard",
            displayName: "Venguard");

        Toast.Activated += Toast_Activated;
    }

    public void ShowRepairNeeded()
    {
        Toast.Show(
            "Vencord needs repair",
            "Discord Stable is no longer patched by Vencord.",
            new ToastOptions
            {
                PrimaryButton = ("Repair", "repair"),
                SecondaryButton = ("Dismiss", "dismiss")
            });
    }

    private void Toast_Activated(ToastActivationArgs args)
    {
        Activated?.Invoke(this, args.Arguments);
    }
}