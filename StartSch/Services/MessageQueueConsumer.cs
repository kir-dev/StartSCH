using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Threading.Channels;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Components.EmailTemplates;
using StartSch.Data;

namespace StartSch.Services;

[Index(nameof(Discriminator), nameof(NotBefore), nameof(Created))]
public abstract class BackgroundTask : IRequest<BackgroundTaskResult>
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? NotBefore { get; set; }

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

public class SendPushNotificationHandler : IRequestHandler<SendPushNotificationRequest, BackgroundTaskResult>
{
    public ValueTask<BackgroundTaskResult> Handle(SendPushNotificationRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(BackgroundTaskResult.HandlerFull);
    }
}

public class BackgroundTaskManager(IServiceProvider serviceProvider, IEnumerable<IBackgroundTaskScheduler> schedulers)
    : BackgroundService
{
    readonly SemaphoreSlim semaphoreSlim = new(1, 1);

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

    // query db
    // send tasks to handlers
    // move overflow tasks to buffers
    // remove task from db once finished
    // query for tasks until all handlers full
    // wait for handler to finish
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int QueryBatchSize = 100;

        var dict = schedulers.ToDictionary(x => x.Type);

        List<(BackgroundTask, Task<BackgroundTaskResult>)> ongoingTasks = [];

        while (true)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            Db db = scope.ServiceProvider.GetRequiredService<Db>();
            List<string> full = buffers.Where((pair => !pair.Value.IsEmpty)).Select(pair => pair.Key.Name).ToList();
            List<BackgroundTask> tasks = await db.BackgroundTasks
                .Where(x => !full.Contains(x.Discriminator))
                .OrderBy(x => x.NotBefore)
                .ThenBy(x => x.Created)
                .Take(QueryBatchSize)
                .ToListAsync(stoppingToken);

            Notify();

            // sort tasks into buffers
            foreach (BackgroundTask backgroundTask in tasks)
            {
                void Handle<TBackgroundTask>(TBackgroundTask backgroundTask)
                {
                }
            }

            // send tasks to handlers
            foreach (var (_, buffer) in buffers)
            {
                while (buffer.TryPeek(out BackgroundTask? backgroundTask))
                {
                    ValueTask<BackgroundTaskResult> valueTask = mediator.Send(backgroundTask, stoppingToken);
                    if (valueTask is { IsCompletedSuccessfully: true, Result: BackgroundTaskResult.HandlerFull })
                        break;
                    buffer.TryDequeue(out _);
                    ongoingTasks.Add((backgroundTask, valueTask.AsTask()));
                }
            }

            List<(BackgroundTask, Task<BackgroundTaskResult>)> failedTasks = [];
            List<BackgroundTask> tasksToDelete = [];
            ongoingTasks.RemoveAll(tuple =>
            {
                (BackgroundTask backgroundTask, var task) = tuple;

                if (task.IsCompleted)
                {
                    if (task is { IsCompletedSuccessfully: true, Result: BackgroundTaskResult.Ok })
                        tasksToDelete.Add(backgroundTask);
                    else
                        failedTasks.Add(tuple);
                    return true;
                }

                return false;
            });

            if (tasksToDelete.Count > 0)
                await db.BackgroundTasks
                    .Where(x => tasksToDelete.Contains(x))
                    .ExecuteDeleteAsync(CancellationToken.None);

            if (failedTasks.Count > 0)
                throw new NotImplementedException("Somebody ought to implement this");

            if (tasks.Count == QueryBatchSize)
                continue;

            if (ongoingTasks.Count != 0)
            {
                await Task.WhenAny(
                    ongoingTasks
                        .Select(x => x.Item2)
                        .Append(semaphoreSlim.WaitAsync(stoppingToken))
                );
                continue;
            }

            await semaphoreSlim.WaitAsync(stoppingToken);
        }
    }
}

public interface IBackgroundTaskHandler<TBackgroundTask> where TBackgroundTask : BackgroundTask
{
    Task Handle(List<TBackgroundTask> batch);
}

public record BackgroundTaskResult(BackgroundTask BackgroundTask, Task Task, bool DeleteHandled);

public class BackgroundTaskSchedulerOptions<TBackgroundTask> where TBackgroundTask : BackgroundTask
{
    public int MaxBatchCount { get; set; }
    public int MaxTasksPerBatch { get; set; }
    public bool HandlesDeletion { get; set; }
}

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

            Task handleTask = handler.Handle(batch);
            await handleTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            foreach (TBackgroundTask backgroundTask in batch)
            {
                BackgroundTaskResult backgroundTaskResult = new(backgroundTask, handleTask, _options.HandlesDeletion);
                // TODO: pass finished task back to manager
            }
        }
    }
}

// how do we skip ongoing tasks?
// - skip types with full handlers
// - skip the rest of ongoing tasks

public class CreateOrderingStartedNotificationsRequestHandler(
    Db db,
    InterestService interestService,
    NotificationService notificationService)
{
    public async Task Handle(CreateOrderingStartedNotificationsRequest request, CancellationToken cancellationToken)
    {
        await notificationService.CreateOrderingStartedNotification(request.PincerOpening);
        db.BackgroundTasks.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SendEmailRequestHandler(IEmailService emailService)
{
    public int Concurrency => 100;

    public async Task Handle(List<SendEmailRequest> requests)
    {
        throw new NotImplementedException();
        // await emailService.Send(
        //     
        // );
    }
}
