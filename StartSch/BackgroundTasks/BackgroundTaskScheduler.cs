using System.Threading.Channels;
using StartSch.Data;

namespace StartSch.BackgroundTasks;

public class BackgroundTaskScheduler<TBackgroundTask>(
    BackgroundTaskSchedulerOptions<TBackgroundTask> options,
    IServiceScopeFactory serviceScopeFactory,
    BackgroundTaskManager backgroundTaskManager
)
    : BackgroundService, IBackgroundTaskScheduler
    where TBackgroundTask : BackgroundTask
{
    private readonly Channel<TBackgroundTask> _channel = Channel.CreateUnbounded<TBackgroundTask>();
    private readonly List<Task> _batches = [];

    public Type Type => typeof(TBackgroundTask);
    public bool IsFull => _batches.Count >= options.MaxBatchCount;
    public void Schedule(BackgroundTask backgroundTask) => _channel.Writer.TryWrite((TBackgroundTask)backgroundTask);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _batches.RemoveAll(x => x.IsCompleted);
            while (IsFull)
            {
                await Task.WhenAny(_batches);
                _batches.RemoveAll(x => x.IsCompleted);
            }

            TBackgroundTask? backgroundTask = await _channel.Reader.ReadAsync(stoppingToken);

            List<TBackgroundTask> batch = [backgroundTask];

            while (true)
            {
                bool canFitMoreInBatch = batch.Count != options.MaxTasksPerBatch;
                bool canCreateMoreBatches = _batches.Count < options.MaxBatchCount - 1;

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
                BackgroundTaskResult backgroundTaskResult = new(backgroundTask, handleTask, options.HandlesDeletion);
                backgroundTaskManager.HandleCompletedTask(backgroundTaskResult);
            }
        }
    }
}
