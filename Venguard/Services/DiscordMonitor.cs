using System.Timers;

namespace Venguard.Services;

public sealed class DiscordMonitor : IDisposable
{
    private readonly DiscordService _discordService;
    private readonly System.Timers.Timer _timer;

    private bool _hasPublishedInitialStatus;
    private bool _started;

    public DiscordStatus CurrentStatus { get; private set; }

    public event EventHandler<DiscordStatus>? StatusChanged;

    public DiscordMonitor(
        DiscordService discordService,
        TimeSpan interval)
    {
        _discordService = discordService;

        CurrentStatus =
            _discordService.GetStatus();

        _timer =
            new System.Timers.Timer(
                interval.TotalMilliseconds)
            {
                AutoReset = true
            };

        _timer.Elapsed +=
            Timer_Elapsed;
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;

        CheckStatus();

        _timer.Start();
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        _started = false;

        _timer.Stop();
    }

    public void CheckNow()
    {
        CheckStatus();
    }

    public void UpdateInterval(
        TimeSpan interval)
    {
        _timer.Interval =
            interval.TotalMilliseconds;

        if (_started)
        {
            _timer.Stop();
            _timer.Start();
        }
    }

    public void Dispose()
    {
        Stop();

        _timer.Elapsed -=
            Timer_Elapsed;

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
        DiscordStatus status;

        try
        {
            status =
                _discordService.GetStatus();
        }
        catch
        {
            return;
        }

        if (!_hasPublishedInitialStatus)
        {
            _hasPublishedInitialStatus =
                true;

            CurrentStatus =
                status;

            StatusChanged?.Invoke(
                this,
                status);

            return;
        }

        if (status ==
            CurrentStatus)
        {
            return;
        }

        CurrentStatus =
            status;

        StatusChanged?.Invoke(
            this,
            status);
    }
}