namespace StartSch.BackgroundTasks;

public interface IBackgroundTaskScheduler
{
    Type Type { get; }
    bool IsFull { get; }
    void Schedule(BackgroundTask backgroundTask);
}
