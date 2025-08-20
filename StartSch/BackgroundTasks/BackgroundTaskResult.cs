using StartSch.Data;

namespace StartSch.BackgroundTasks;

public record BackgroundTaskResult(
    BackgroundTask BackgroundTask,
    Task Task,
    bool DeleteHandled
);
