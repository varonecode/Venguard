using System.Timers;

namespace Venguard.Services;

public sealed class DiscordMonitor : IDisposable
{
    private readonly DiscordService _discordService;
    private readonly System.Timers.Timer _timer;
    private bool _hasPublishedInitialStatus;

    public DiscordStatus CurrentStatus { get; private set; }

    public event EventHandler<DiscordStatus>? StatusChanged;

    public DiscordMonitor(
        DiscordService discordService,
        TimeSpan interval)
    {
        _discordService = discordService;
        CurrentStatus = _discordService.GetStatus();

        _timer = new System.Timers.Timer(interval.TotalMilliseconds);
        _timer.Elapsed += Timer_Elapsed;
    }

    public void Start()
    {
        CheckStatus();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void Timer_Elapsed(
        object? sender,
        ElapsedEventArgs e)
    {
        CheckStatus();
    }

    private void CheckStatus()
    {
        var status = _discordService.GetStatus();

        if (!_hasPublishedInitialStatus)
        {
            _hasPublishedInitialStatus = true;
            CurrentStatus = status;
            StatusChanged?.Invoke(this, status);
            return;
        }

        if (status == CurrentStatus)
        {
            return;
        }

        CurrentStatus = status;
        StatusChanged?.Invoke(this, status);
    }
}