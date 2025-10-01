using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.BackgroundTasks;

public class BackgroundTaskManager(
    IServiceProvider serviceProvider,
    IDbContextFactory<Db> dbFactory,
    ILogger<BackgroundTaskManager> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly Channel<BackgroundTaskResult> completedTasks = Channel.CreateUnbounded<BackgroundTaskResult>();

    /// Signals to the BackgroundTaskManager that there might be new tasks in the DB.
    public void Notify()
    {
        if (semaphore.CurrentCount == 1)
            return;
        logger.LogTrace("Notifying background task manager of new tasks");
        try
        {
            semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public void HandleCompletedTask(BackgroundTaskResult result) => completedTasks.Writer.TryWrite(result);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int QueryBatchSize = 100;

        List<IBackgroundTaskScheduler> schedulers = serviceProvider
            .GetRequiredService<IEnumerable<IBackgroundTaskScheduler>>().ToList();
        Dictionary<Type, IBackgroundTaskScheduler> typeToScheduler = schedulers.ToDictionary(x => x.Type);
        HashSet<IBackgroundTaskScheduler> failedSchedulers = [];
        HashSet<BackgroundTask> ongoingTasks = [];
        List<BackgroundTask> tasksToDelete = [];
        DateTime? nextScheduledTask = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            // check for tasks in the db
            if (semaphore.CurrentCount > 0)
            {
                nextScheduledTask = null;
                await semaphore.WaitAsync(stoppingToken);
                logger.LogDebug("Checking for new background tasks");

                await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
                List<string> skippedTypes = schedulers
                    .Where(x => x.IsFull)
                    .Union(failedSchedulers)
                    .Select(x => x.Type.Name)
                    .ToList();
                if (skippedTypes.Count > 0)
                {
                    var full = schedulers.Where(x => x.IsFull).ToList();
                    if (full.Count > 0)
                        logger.LogInformation("Skipping full handlers: {SkippedTypes}", string.Join(", ", full));
                    if (failedSchedulers.Count > 0)
                        logger.LogInformation(
                            "Skipping failed handlers: {SkippedTypes}", string.Join(", ", failedSchedulers));
                }

                DateTime utcNow = DateTime.UtcNow;
                List<BackgroundTask> tasks = await db.BackgroundTasks
                    .Where(x =>
                        !skippedTypes.Contains(x.Discriminator)
                        && !ongoingTasks.Contains(x)
                        && (x.WaitUntil == null || x.WaitUntil <= utcNow)
                    )
                    .OrderBy(x => x.WaitUntil)
                    .ThenBy(x => x.Created)
                    .Take(QueryBatchSize)
                    .ToListAsync(stoppingToken);

                if (tasks.Count > 0)
                    logger.LogDebug("Found {Count} tasks, scheduling them now", tasks.Count);

                foreach (BackgroundTask backgroundTask in tasks)
                {
                    logger.LogTrace("Scheduling task {Type}#{Id}", backgroundTask.Discriminator, backgroundTask.Id);
                    typeToScheduler[backgroundTask.GetType()].Schedule(backgroundTask);
                    ongoingTasks.Add(backgroundTask);
                }

                // got a full batch, there is probably more in the db
                if (tasks.Count == QueryBatchSize)
                    Notify();
                // partial batch, no more work currently, check scheduled tasks
                else
                {
                    nextScheduledTask = await db.BackgroundTasks
                        .Where(x =>
                            !skippedTypes.Contains(x.Discriminator)
                            && !ongoingTasks.Contains(x)
                        )
                        .OrderBy(x => x.WaitUntil)
                        .Select(x => x.WaitUntil)
                        .FirstOrDefaultAsync(stoppingToken);
                    
                    if (nextScheduledTask.HasValue)
                        logger.LogTrace("No more tasks, next scheduled task is due at {Time}", nextScheduledTask);
                    else
                        logger.LogTrace("No more tasks, no more scheduled tasks");
                }
            }

            // handle completed tasks
            while (completedTasks.Reader.TryRead(out BackgroundTaskResult? completedTask))
            {
                Debug.Assert(completedTask.Task.IsCompleted);

                logger.LogTrace("Handling completed task {Type}#{Id}", completedTask.BackgroundTask.Discriminator,
                    completedTask.BackgroundTask.Id);
                ongoingTasks.Remove(completedTask.BackgroundTask);

                if (!completedTask.Task.IsCompletedSuccessfully)
                {
                    if (failedSchedulers.Add(typeToScheduler[completedTask.BackgroundTask.GetType()]))
                        logger.LogError(
                            completedTask.Task.Exception,
                            "Handler failed for {} BackgroundTask",
                            completedTask.BackgroundTask.Discriminator);
                    continue;
                }

                if (!completedTask.DeleteHandled)
                    tasksToDelete.Add(completedTask.BackgroundTask);
            }

            // delete completed tasks that weren't deleted by their handlers
            if (tasksToDelete.Count > 0)
            {
                logger.LogDebug("Deleting {Count} completed tasks", tasksToDelete.Count);
                await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
                await db.BackgroundTasks
                    .Where(x => tasksToDelete.Contains(x))
                    .ExecuteDeleteAsync(CancellationToken.None);
                tasksToDelete.Clear();
            }

            // wait if there is no more work
            bool haveCompletedTasks = completedTasks.Reader.TryPeek(out _);
            bool notified = semaphore.CurrentCount > 0;
            if (!haveCompletedTasks && !notified)
            {
                logger.LogTrace("No more work to do, waiting for new tasks or for a scheduled task to be due");
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                Task waitForNotification = semaphore.WaitAsync(cts.Token);
                Task waitForCompletedTasks = completedTasks.Reader.WaitToReadAsync(cts.Token).AsTask();
                List<Task> tasks = [waitForNotification, waitForCompletedTasks];
                if (nextScheduledTask.HasValue)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    TimeSpan waitFor = nextScheduledTask.Value - utcNow;
                    if (waitFor < TimeSpan.FromSeconds(1))
                        tasks.Add(Task.CompletedTask);
                    else
                    {
                        logger.LogTrace("Waiting until {Time} for next scheduled task", nextScheduledTask.Value);
                        tasks.Add(Task.Delay(waitFor, cts.Token));
                    }
                }
                
                Task completedTask = await Task.WhenAny(tasks);
                
                // a notification came in, or it's time to handle the scheduled task
                if (completedTask != waitForCompletedTasks)
                    Notify();
                
                await cts.CancelAsync();
            }
        }
    }
}
