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
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private readonly Channel<BackgroundTaskResult> results = Channel.CreateUnbounded<BackgroundTaskResult>();

    /// Signals to the BackgroundTaskManager that there are new tasks in the DB.
    public void Notify()
    {
        if (semaphoreSlim.CurrentCount == 1)
            return;
        try
        {
            semaphoreSlim.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public void HandleResult(BackgroundTaskResult result) => results.Writer.TryWrite(result);

    // TODO:
    // - [x] store and skip ongoing tasks
    // - [x] handle finished tasks
    // - [x] disable handlers with failed tasks
    // - [x] delete successful tasks
    // - [x] skip tasks that are scheduled for later
    // - wait for
    //   - next upcoming task
    //   - notification that new tasks have been added
    //   - completed tasks?

    // query db
    // send tasks to handlers
    // move overflow tasks to buffers
    // remove task from db once finished
    // query for tasks until all handlers full
    // wait for handler to finish
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int QueryBatchSize = 100;

        List<IBackgroundTaskScheduler> schedulers = serviceProvider
            .GetRequiredService<IEnumerable<IBackgroundTaskScheduler>>().ToList();
        Dictionary<Type, IBackgroundTaskScheduler> typeToScheduler = schedulers.ToDictionary(x => x.Type);
        HashSet<IBackgroundTaskScheduler> failedSchedulers = [];
        HashSet<BackgroundTask> ongoingTasks = [];
        List<BackgroundTask> tasksToDelete = [];
        DateTime? nextScheduledEvent = null;

        // TASKS:
        // - get tasks from db and send them to handlers
        // - delete finished tasks
        // - disable failing handlers

        while (!stoppingToken.IsCancellationRequested)
        {
            if (semaphoreSlim.CurrentCount > 0)
            {
                nextScheduledEvent = null;
                await semaphoreSlim.WaitAsync(stoppingToken);

                await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
                List<string> skippedTypes = schedulers
                    .Where(x => x.IsFull)
                    .Union(failedSchedulers)
                    .Select(x => x.Type.Name)
                    .ToList();
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

                foreach (BackgroundTask backgroundTask in tasks)
                {
                    typeToScheduler[backgroundTask.GetType()].Schedule(backgroundTask);
                    ongoingTasks.Add(backgroundTask);
                }

                // got a full batch, there is probably more in the db
                if (tasks.Count == QueryBatchSize)
                    Notify();
                // partial batch, no work currently, check scheduled tasks
                else
                {
                    nextScheduledEvent = await db.BackgroundTasks
                        .Where(x =>
                            !skippedTypes.Contains(x.Discriminator)
                            && !ongoingTasks.Contains(x)
                        )
                        .OrderBy(x => x.WaitUntil)
                        .Select(x => x.WaitUntil)
                        .FirstOrDefaultAsync(stoppingToken);
                }
            }

            while (results.Reader.TryRead(out BackgroundTaskResult? completedTask))
            {
                ongoingTasks.Remove(completedTask.BackgroundTask);

                Debug.Assert(completedTask.Task.IsCompleted);

                if (!completedTask.Task.IsCompletedSuccessfully)
                {
                    if (failedSchedulers.Add(typeToScheduler[completedTask.BackgroundTask.GetType()]))
                        logger.LogError(
                            completedTask.Task.Exception,
                            "Handler failed for {} BackgroundTask",
                            completedTask.BackgroundTask.Discriminator);
                    continue;
                }

                if (completedTask is { DeleteHandled: false })
                    tasksToDelete.Add(completedTask.BackgroundTask);
            }

            if (tasksToDelete.Count > 0)
            {
                await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
                await db.BackgroundTasks
                    .Where(x => tasksToDelete.Contains(x))
                    .ExecuteDeleteAsync(CancellationToken.None);
                tasksToDelete.Clear();
            }

            bool haveCompletedTasks = results.Reader.TryPeek(out _);
            bool notified = semaphoreSlim.CurrentCount > 0;

            if (!haveCompletedTasks && !notified)
            {
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                Task waitForNotification = semaphoreSlim.WaitAsync(cts.Token);
                Task waitForCompletedTasks = results.Reader.WaitToReadAsync(cts.Token).AsTask();
                List<Task> tasks = [waitForNotification, waitForCompletedTasks];
                if (nextScheduledEvent.HasValue)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    TimeSpan waitFor = nextScheduledEvent.Value - utcNow;
                    if (waitFor < TimeSpan.FromSeconds(1))
                        tasks.Add(Task.CompletedTask);
                    else
                        tasks.Add(Task.Delay(nextScheduledEvent.Value - utcNow, cts.Token));
                }
                
                Task completedTask = await Task.WhenAny(tasks);
                
                await cts.CancelAsync();
                if (completedTask != waitForCompletedTasks)
                    Notify();
            }
        }
    }
}
