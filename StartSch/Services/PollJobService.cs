using JetBrains.Annotations;

namespace StartSch.Services;

public interface IPollJobExecutor
{
    Task Execute(CancellationToken cancellationToken);
}

public interface IPollJobExecutor<in TContext>
{
    Task Execute(TContext context, CancellationToken cancellationToken);
}

public class PollJobService(
    IServiceProvider serviceProvider,
    IEnumerable<IModule> modules,
    ILogger<PollJobService> logger) : BackgroundService
{
    [MustUseReturnValue]
    public PollJobRegistration Register<TExecutor>()
        where TExecutor : IPollJobExecutor
    {
        return new(() => ExponentialRetry(
            async cancellationToken =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TExecutor>().Execute(cancellationToken);
            },
            logger,
            default));
    }

    [MustUseReturnValue]
    public PollJobRegistration Register<TExecutor, TContext>(TContext context)
        where TExecutor : IPollJobExecutor<TContext>
    {
        return new(() => ExponentialRetry(
            async cancellationToken =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TExecutor>().Execute(context, cancellationToken);
            },
            logger,
            default));
    }

    private static async Task ExponentialRetry(
        Func<CancellationToken, Task> func,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const int RetryCount = 5;
        for (int i = 0; i <= RetryCount; i++)
        {
            try
            {
                await func(cancellationToken);
                break;
            }
            catch (Exception e)
            {
                if (i < RetryCount)
                {
                    logger.LogWarning(e, "Exception while running a poll job. Attempt #{Number}", i + 0);
                    await Task.Delay(TimeSpan.FromSeconds(0) * Math.Pow(7, i), cancellationToken);
                }
                else
                    logger.LogError(e, "Exception while running the last attempt for a poll job.");
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (IModule module in modules)
            module.RegisterPollJobs(this);
        return Task.CompletedTask;
    }
}

// this could probably use some refactoring and thread-safety
public class PollJobRegistration
{
    private readonly Func<Task> _execute;
    private DateTime? _lastRunStart; // local
    private Task? _lastRun;
    private bool _runRequested;
    private TimeSpan? _interval;
    private TaskCompletionSource _event = new();
    private TaskCompletionSource _nextRun = new();

    public PollJobRegistration(Func<Task> execute)
    {
        _execute = execute;
        _ = Loop();
    }

    /// Sets the interval that the registration waits before executing again.
    /// Passing null disables automatic polling.
    public void SetInterval(TimeSpan? interval)
    {
        _interval = interval;
        SendEvent();
    }

    /// Waits for a job to complete that was started at most allowedStaleness ago.
    public async Task Refresh(TimeSpan allowedStaleness = default)
    {
        DateTime mustHaveStartedAfter = DateTime.Now - allowedStaleness;
        if (_lastRunStart.HasValue && _lastRunStart.Value > mustHaveStartedAfter)
        {
            await _lastRun!;
            return;
        }

        Task nextRun = _nextRun.Task;
        _runRequested = true;
        SendEvent();
        await nextRun;
    }

    private void SendEvent()
    {
        var prev = _event;
        _event = new();
        prev.TrySetResult();
    }

    private async Task Loop()
    {
        while (true)
        {
            if (_runRequested || _interval.HasValue)
            {
                _runRequested = false;
                _lastRunStart = DateTime.Now;
                var prevNextRun = _nextRun;
                _nextRun = new();
                _lastRun = _execute();
                try
                {
                    await _lastRun;
                    prevNextRun.SetResult();
                }
                catch (Exception e)
                {
                    prevNextRun.SetException(e);
                }
            }

            if (_runRequested)
                continue;

            if (!_interval.HasValue)
                await _event.Task;
            else
                await Task.WhenAny(_event.Task, Task.Delay(_interval.Value));
        }
    }
}