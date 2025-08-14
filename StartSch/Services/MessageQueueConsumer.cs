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
    HandlerFull
}

[Index("Discriminator", nameof(NotBefore), nameof(Created))]
public abstract class BackgroundTask : IRequest<BackgroundTaskResult>
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? NotBefore { get; set; }
}

public class SendEmail : SendNotification;

public class SendPushNotification : SendNotification;

public class CreateEventStartedNotifications : BackgroundTask
{
    public Event Event { get; set; }
}

public class CreateOrderingStartedNotifications : BackgroundTask
{
    public PincerOpening PincerOpening { get; set; }
}

public abstract class SendNotification : BackgroundTask
{
    public User User { get; set; }
    public Notification Notification { get; set; }
}

public class CreatePostPublishedNotifications : BackgroundTask
{
    public Post Post { get; set; }
}

public class SendPushNotificationHandler : IRequestHandler<SendPushNotification, BackgroundTaskResult>
{
    public ValueTask<BackgroundTaskResult> Handle(SendPushNotification request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(BackgroundTaskResult.HandlerFull);
    }
}

public class SendEmailHandler(IEmailService emailService, BlazorTemplateRenderer blazorTemplateRenderer)
    : BackgroundService, IRequestHandler<SendEmail>
{
    private readonly Channel<SendEmail> _requests = Channel.CreateBounded<SendEmail>(100);

    public ValueTask<Unit> Handle(SendEmail request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(Unit.Value);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _requests.Reader.WaitToReadAsync(stoppingToken))
        {
            List<SendEmail> batch = [];
            while (batch.Count < 100 && _requests.Reader.TryRead(out SendEmail? email))
                batch.Add(email);
            await Task.WhenAll(
                batch
                    .GroupBy(x => x.Notification)
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

                        await emailService.Send(new MultipleSendRequestDto(
                            new("", ""),
                            g.Select(x => x.User.GetVerifiedEmailAddress()!)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList(),
                            "",
                            content
                        ));
                    })
            );
        }
    }
}

public class BackgroundTaskManager(IServiceProvider serviceProvider, IMediator mediator) : BackgroundService
{
    readonly SemaphoreSlim semaphoreSlim = new(1,1);
    
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
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int queryBatchSize = 50;
        
        ConcurrentDictionary<Type, ConcurrentQueue<BackgroundTask>> buffers = [];

        while (true)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            Db db = scope.ServiceProvider.GetRequiredService<Db>();
            List<BackgroundTask> tasks = await db.BackgroundTasks
                .OrderBy(x => x.NotBefore)
                .ThenBy(x => x.Created)
                .Take(queryBatchSize)
                .ToListAsync(stoppingToken);
            var groups = tasks
                .GroupBy(x => x.GetType())
                .ToList();

            foreach (var group in groups)
            {
                Type taskType = group.Key;
                var backgroundTasks = group.ToList();
                for (int i = 0; i < backgroundTasks.Count; i++)
                {
                    BackgroundTask backgroundTask = backgroundTasks[i];
                    Task<BackgroundTaskResult> task = mediator.Send(backgroundTask, stoppingToken).AsTask();
                    
                    // handler full, store tasks in buffer
                    if (task is { IsCompleted: true, Result: BackgroundTaskResult.HandlerFull })
                    {
                        var buffer = buffers.GetOrAdd(taskType, (type) => []);
                        for (; i < backgroundTasks.Count; i++)
                            buffer.Enqueue(backgroundTasks[i]);
                        break;
                    }
                }
            }
            
            switch (result)
            {
                case BackgroundTaskResult.Ok:
                    // await db.ExecuteDelete
                    break;
                case BackgroundTaskResult.HandlerFull:
                    fullHandlers[]
                    break;
                default:
                    throw new();
            }
            
            // no more tasks in the db, wait for a notification that new tasks have been added
            // TODO: also wait unfinished tasks
            if (tasks.Count != queryBatchSize)
                await semaphoreSlim.WaitAsync(stoppingToken);
        }
        
    }
}
