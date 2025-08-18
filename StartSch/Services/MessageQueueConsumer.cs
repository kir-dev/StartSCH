using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch.Services;

[Index(nameof(Discriminator), nameof(ProcessAfter), nameof(Created))]
public abstract class BackgroundTask
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? ProcessAfter { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Discriminator { get; set; } = null!;
}

public class SendEmailRequest : SendNotificationRequest;

public class SendPushNotificationRequest : SendNotificationRequest;

public class CreateEventStartedNotifications : BackgroundTask
{
    public Event Event { get; set; }
}

public class CreateOrderingStartedNotificationsRequest : BackgroundTask
{
    public PincerOpening PincerOpening { get; set; }
}

public abstract class SendNotificationRequest : BackgroundTask
{
    public User User { get; set; }
    public Notification Notification { get; set; }
}

public class CreatePostPublishedNotificationsRequest : BackgroundTask
{
    public Post Post { get; set; }
}

public class BackgroundTaskManager(
    IServiceProvider serviceProvider,
    IEnumerable<IBackgroundTaskScheduler> schedulers,
    IDbContextFactory<Db> dbFactory
)
    : BackgroundService
{
    readonly SemaphoreSlim semaphoreSlim = new(1, 1);
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

        List<IBackgroundTaskScheduler> allSchedulers = schedulers.ToList();
        Dictionary<Type, IBackgroundTaskScheduler> typeToScheduler = allSchedulers.ToDictionary(x => x.Type);
        HashSet<IBackgroundTaskScheduler> failedSchedulers = [];
        HashSet<BackgroundTask> ongoingTasks = [];
        BackgroundTask? nextUpcomingTask = null;
        bool moreInDb = true;
        List<BackgroundTask> tasksToDelete = [];
        
        
        // TASKS:
        // - get tasks from db and send them to handlers
        // - delete finished tasks
        // - disable failing handlers

        while (!stoppingToken.IsCancellationRequested)
        {
            if (moreInDb)
            {
                moreInDb = false;

                await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
                List<string> skippedTypes = allSchedulers
                    .Where(x => x.IsFull)
                    .Union(failedSchedulers)
                    .Select(x => x.Type.Name)
                    .ToList();
                DateTime utcNow = DateTime.UtcNow;
                List<BackgroundTask> tasks = await db.BackgroundTasks
                    .Where(x =>
                        !skippedTypes.Contains(x.Discriminator)
                        && !ongoingTasks.Contains(x)
                        && (x.ProcessAfter == null || x.ProcessAfter <= utcNow)
                    )
                    .OrderBy(x => x.ProcessAfter)
                    .ThenBy(x => x.Created)
                    .Take(QueryBatchSize)
                    .ToListAsync(stoppingToken);

                if (tasks.Count == QueryBatchSize)
                    moreInDb = true;

                foreach (BackgroundTask backgroundTask in tasks)
                    typeToScheduler[backgroundTask.GetType()].Schedule(backgroundTask);
            }

            while (results.Reader.TryRead(out BackgroundTaskResult? completedTask))
            {
                ongoingTasks.Remove(completedTask.BackgroundTask);

                Debug.Assert(completedTask.Task.IsCompleted);

                if (!completedTask.Task.IsCompletedSuccessfully)
                {
                    failedSchedulers.Add(typeToScheduler[completedTask.BackgroundTask.GetType()]);
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
            
            
        }
    }
}

public static class ServiceCollectionExtensions
{
    public static void AddScopedBackgroundTaskHandler<TBackgroundTask, THandler>(
        this IServiceCollection serviceCollection,
        int maxBatchCount = 1,
        int maxTasksPerBatch = 1,
        bool handlesDeletion = false
    )
        where TBackgroundTask : BackgroundTask
        where THandler : class, IBackgroundTaskHandler<TBackgroundTask>
    {
        serviceCollection.AddScoped<IBackgroundTaskHandler<TBackgroundTask>, THandler>();
        serviceCollection.AddBackgroundTaskHandler<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion);
    }
    
    public static void AddSingletonBackgroundTaskHandler<TBackgroundTask, THandler>(
        this IServiceCollection serviceCollection,
        int maxBatchCount = 1,
        int maxTasksPerBatch = 1,
        bool handlesDeletion = false
    )
        where TBackgroundTask : BackgroundTask
        where THandler : class, IBackgroundTaskHandler<TBackgroundTask>
    {
        serviceCollection.AddSingleton<IBackgroundTaskHandler<TBackgroundTask>, THandler>();
        serviceCollection.AddBackgroundTaskHandler<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion);
    }

    private static void AddBackgroundTaskHandler<TBackgroundTask>(
        this IServiceCollection serviceCollection,
        int maxBatchCount,
        int maxTasksPerBatch,
        bool handlesDeletion
    )
        where TBackgroundTask : BackgroundTask
    {
        if (maxBatchCount < 1 || maxTasksPerBatch < 1)
            throw new();
        serviceCollection.AddSingleton(
            new BackgroundTaskSchedulerOptions<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion));
        serviceCollection.AddSingleton<BackgroundTaskScheduler<TBackgroundTask>>();
        serviceCollection.AddSingleton<IBackgroundTaskScheduler>(sp =>
            sp.GetRequiredService<BackgroundTaskScheduler<TBackgroundTask>>());
        serviceCollection.AddSingleton<IHostedService>(sp =>
            sp.GetRequiredService<BackgroundTaskScheduler<TBackgroundTask>>());
    }
}

public interface IBackgroundTaskHandler<TBackgroundTask> where TBackgroundTask : BackgroundTask
{
    Task Handle(List<TBackgroundTask> batch, CancellationToken cancellationToken);
}

public record BackgroundTaskResult(BackgroundTask BackgroundTask, Task Task, bool DeleteHandled);

// ReSharper disable once UnusedTypeParameter
public record BackgroundTaskSchedulerOptions<TBackgroundTask>(
    int MaxBatchCount,
    int MaxTasksPerBatch,
    bool HandlesDeletion
) where TBackgroundTask : BackgroundTask;

public interface IBackgroundTaskScheduler
{
    Type Type { get; }
    bool IsFull { get; }
    void Schedule(BackgroundTask backgroundTask);
}

public class BackgroundTaskScheduler<TBackgroundTask>(
    IOptions<BackgroundTaskSchedulerOptions<TBackgroundTask>> options,
    IServiceScopeFactory serviceScopeFactory,
    BackgroundTaskManager backgroundTaskManager
)
    : BackgroundService, IBackgroundTaskScheduler
    where TBackgroundTask : BackgroundTask
{
    private readonly Channel<TBackgroundTask> _channel = Channel.CreateUnbounded<TBackgroundTask>();
    private readonly BackgroundTaskSchedulerOptions<TBackgroundTask> _options = options.Value;
    private readonly List<Task> _batches = [];

    public Type Type => typeof(TBackgroundTask);
    public bool IsFull => _batches.Count >= _options.MaxBatchCount;
    public void Schedule(BackgroundTask backgroundTask) => _channel.Writer.TryWrite((TBackgroundTask)backgroundTask);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _batches.RemoveAll(x => x.IsCompleted);
            if (_batches.Count >= _options.MaxBatchCount)
                await Task.WhenAny(_batches);

            TBackgroundTask? backgroundTask = await _channel.Reader.ReadAsync(stoppingToken);

            List<TBackgroundTask> batch = [backgroundTask];

            while (true)
            {
                bool canFitMoreInBatch = batch.Count != _options.MaxTasksPerBatch;
                bool canCreateMoreBatches = _batches.Count < _options.MaxBatchCount - 1;

                if (!canFitMoreInBatch && !canCreateMoreBatches)
                    break;

                if (!_channel.Reader.TryRead(out backgroundTask))
                    break;

                if (canFitMoreInBatch)
                {
                    batch.Add(backgroundTask);
                    continue;
                }

                _batches.Add(HandleBatch(batch));
                batch = [backgroundTask];
            }

            _batches.Add(HandleBatch(batch));
        }

        return;

        async Task HandleBatch(List<TBackgroundTask> batch)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IBackgroundTaskHandler<TBackgroundTask>>();

            Task handleTask = handler.Handle(batch, stoppingToken);
            await handleTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            foreach (TBackgroundTask backgroundTask in batch)
            {
                BackgroundTaskResult backgroundTaskResult = new(backgroundTask, handleTask, _options.HandlesDeletion);
                backgroundTaskManager.HandleResult(backgroundTaskResult);
            }
        }
    }
}

public class CreateOrderingStartedNotificationsRequestHandler(
    Db db,
    NotificationService notificationService
)
    : IBackgroundTaskHandler<CreateOrderingStartedNotificationsRequest>
{

    public async Task Handle(List<CreateOrderingStartedNotificationsRequest> batch, CancellationToken cancellationToken)
    {
        var request = batch.Single();
        await notificationService.CreateOrderingStartedNotification(request.PincerOpening);
        db.BackgroundTasks.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SendEmailRequestHandler(IEmailService emailService)
    : IBackgroundTaskHandler<SendEmailRequest>
{
    public async Task Handle(List<SendEmailRequest> batch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // TODO: load relationships, group by notification, send
    }
}
