using System.Collections.Concurrent;
using System.Threading.Channels;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StartSch.Components.EmailTemplates;
using StartSch.Data;

namespace StartSch.Services;

public enum BackgroundTaskResult
{
    Ok,
    OkWithTaskDeleted,
    HandlerFull
}

[Index(nameof(Discriminator), nameof(NotBefore), nameof(Created))]
public abstract class BackgroundTask : IRequest<BackgroundTaskResult>
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? NotBefore { get; set; }
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
    public ValueTask<BackgroundTaskResult> Handle(SendPushNotificationRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(BackgroundTaskResult.HandlerFull);
    }
}

public class SendEmailHandler(IEmailService emailService, BlazorTemplateRenderer blazorTemplateRenderer)
    : BackgroundService, IRequestHandler<SendEmailRequest, BackgroundTaskResult>
{
    record RequestCompletionSource(SendEmailRequest Request)
    {
        private TaskCompletionSource<BackgroundTaskResult>? _taskCompletionSource = null;

        public Task<BackgroundTaskResult> GetTask()
        {
            if (_taskCompletionSource != null)
                throw new InvalidOperationException();
            _taskCompletionSource = new();
            return _taskCompletionSource.Task;
        }

        public void Complete(Task<BackgroundTaskResult> task) => _taskCompletionSource!.SetFromTask(task);
    }
    
    private readonly Channel<RequestCompletionSource> _channel = Channel.CreateBounded<RequestCompletionSource>(100);

    public ValueTask<BackgroundTaskResult> Handle(SendEmailRequest request, CancellationToken cancellationToken)
    {
        RequestCompletionSource requestCompletionSource = new(request);
        if (!_channel.Writer.TryWrite(requestCompletionSource))
            return ValueTask.FromResult(BackgroundTaskResult.HandlerFull);

        return new(requestCompletionSource.GetTask());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            List<RequestCompletionSource> batch = [];
            while (batch.Count < 100 && _channel.Reader.TryRead(out RequestCompletionSource? idk))
                batch.Add(idk);
            await Task.WhenAll(
                batch
                    .GroupBy(x => x.Request.Notification)
                    .Select(async g =>
                    {
                        Notification notification = g.Key;
                        string content = await (notification switch
                        {
                            PostNotification postNotification => blazorTemplateRenderer.Render<PostEmailTemplate>(new()
                            {
                                { nameof(PostEmailTemplate.Post), postNotification.Post },
                            }),
                            OrderingStartedNotification orderingStartedNotification =>
                                blazorTemplateRenderer.Render<OrderingStartedEmailTemplate>(
                                    new()
                                    {
                                        {
                                            nameof(OrderingStartedEmailTemplate.PincerOpening),
                                            orderingStartedNotification.Opening
                                        },
                                    }
                                ),
                            _ => throw new NotImplementedException(),
                        });

                        var task = HandleResult(emailService.Send(new MultipleSendRequestDto(
                            new("", ""),
                            g.Select(x => x.Request.User.GetVerifiedEmailAddress()!)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList(),
                            "",
                            content
                        )));
                        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                        foreach (RequestCompletionSource completionSource in g)
                            completionSource.Complete(task);
                        return;

                        async Task<BackgroundTaskResult> HandleResult(Task t)
                        {
                            await t;
                            return BackgroundTaskResult.Ok;
                        }
                    })
            );
        }
    }
}

public class BackgroundTaskManager(IServiceProvider serviceProvider, IMediator mediator) : BackgroundService
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
        const int QueryBatchSize = 50;

        ConcurrentDictionary<Type, ConcurrentQueue<BackgroundTask>> buffers = [];
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
                Type taskType = backgroundTask.GetType();
                var buffer = buffers.GetOrAdd(taskType, _ => []);
                buffer.Enqueue(backgroundTask);
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
