namespace StartSch.BackgroundTasks;

public record BackgroundTaskSchedulerOptions<TBackgroundTask>(
    int MaxBatchCount,
    int MaxTasksPerBatch,
    bool HandlesDeletion
) where TBackgroundTask : BackgroundTask;
