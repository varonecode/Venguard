using WindowsToastNotifyApi;

namespace Venguard.Services;

public sealed class NotificationService
{
    private static readonly TimeSpan NotificationCooldown =
        TimeSpan.FromMinutes(10);

    private readonly object _syncRoot = new();

    private DateTimeOffset? _lastRepairNotification;

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
        lock (_syncRoot)
        {
            if (_lastRepairNotification.HasValue &&
                DateTimeOffset.UtcNow -
                _lastRepairNotification.Value <
                NotificationCooldown)
            {
                return;
            }

            _lastRepairNotification =
                DateTimeOffset.UtcNow;
        }

        Toast.Show(
            "Vencord needs repair",
            "Discord Stable is no longer patched by Vencord.",
            new ToastOptions
            {
                PrimaryButton =
                    ("Repair", "repair"),

                SecondaryButton =
                    ("Dismiss", "dismiss")
            });
    }

    public void ResetRepairNotificationCooldown()
    {
        lock (_syncRoot)
        {
            _lastRepairNotification = null;
        }
    }

    private void Toast_Activated(
        ToastActivationArgs args)
    {
        Activated?.Invoke(
            this,
            args.Arguments);
    }
}