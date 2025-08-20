using JetBrains.Annotations;
using StartSch.Data;

namespace StartSch.BackgroundTasks;

[UsedImplicitly(ImplicitUseKindFlags.Access)]
public record BackgroundTaskSchedulerOptions<TBackgroundTask>(
    int MaxBatchCount,
    int MaxTasksPerBatch,
    bool HandlesDeletion
) where TBackgroundTask : BackgroundTask;
