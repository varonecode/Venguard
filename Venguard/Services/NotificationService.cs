using WindowsToastNotifyApi;

namespace Venguard.Services;

public sealed class NotificationService
{
    public NotificationService()
    {
        Toast.Initialize(
            appId: "Venguard",
            displayName: "Venguard");
    }

    public void ShowRepairNeeded()
    {
        Toast.Show(
            "Vencord needs repair",
            "Discord Stable is no longer patched by Vencord.",
            new ToastOptions
            {
                PrimaryButton = ("Repair", "repair"),
                SecondaryButton = ("Dismiss", "dismiss"),
                Payload = new Dictionary<string, string>
                {
                    ["action"] = "repair"
                }
            });
    }
}