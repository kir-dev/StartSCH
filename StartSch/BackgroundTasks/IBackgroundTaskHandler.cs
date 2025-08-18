using StartSch.Data;

namespace StartSch.BackgroundTasks;

public interface IBackgroundTaskHandler<TBackgroundTask> where TBackgroundTask : BackgroundTask
{
    Task Handle(List<TBackgroundTask> batch, CancellationToken cancellationToken);
}
