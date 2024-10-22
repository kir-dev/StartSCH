namespace StartSch.Services;

public class CronService(IEnumerable<IModule> modules, ILogger<CronService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(modules
            .SelectMany(m => m.CronJobs)
            .Select(job =>
                Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        DateTimeOffset nextTime = await ExponentialRetry(job, logger, stoppingToken);
                        if (nextTime == default)
                            return;

                        var currentTime = DateTimeOffset.Now;
                        var diff = nextTime - currentTime;
                        await Task.Delay(diff, stoppingToken);
                    }
                }, stoppingToken)
            )
        );

    private static async Task<T?> ExponentialRetry<T>(Func<CancellationToken, Task<T>> func, ILogger logger, CancellationToken cancellationToken)
    {
        T? result = default;
        const int RetryCount = 6;
        for (int i = 1; i <= RetryCount; i++)
        {
            try
            {
                result = await func(cancellationToken);
            }
            catch (Exception e)
            {
                if (i < RetryCount)
                {
                    logger.LogWarning(e, "Exception while running a cron job. Attempt #{Number}", i + 1);
                    await Task.Delay(TimeSpan.FromSeconds(1) * Math.Pow(7, i), cancellationToken);
                }
                else
                    logger.LogError(e, "Exception while running the last attempt for a cron job.");
            }
        }

        return result;
    }
}