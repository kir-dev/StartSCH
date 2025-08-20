using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StartSch.Data;

namespace StartSch.BackgroundTasks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScopedBackgroundTaskHandler<
        TBackgroundTask,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
        THandler
    >(
        this IServiceCollection serviceCollection,
        int maxBatchCount = 1,
        int maxTasksPerBatch = 1,
        bool handlesDeletion = false
    )
        where TBackgroundTask : BackgroundTask
        where THandler : class, IBackgroundTaskHandler<TBackgroundTask>
    {
        return serviceCollection
            .AddScoped<IBackgroundTaskHandler<TBackgroundTask>, THandler>()
            .AddBackgroundTaskHandler<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion);
    }

    public static IServiceCollection AddSingletonBackgroundTaskHandler<
        TBackgroundTask,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
        THandler
    >(
        this IServiceCollection serviceCollection,
        int maxBatchCount = 1,
        int maxTasksPerBatch = 1,
        bool handlesDeletion = false
    )
        where TBackgroundTask : BackgroundTask
        where THandler : class, IBackgroundTaskHandler<TBackgroundTask>
    {
        return serviceCollection
            .AddSingleton<IBackgroundTaskHandler<TBackgroundTask>, THandler>()
            .AddBackgroundTaskHandler<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion);
    }

    private static IServiceCollection AddBackgroundTaskHandler<TBackgroundTask>(
        this IServiceCollection serviceCollection,
        int maxBatchCount,
        int maxTasksPerBatch,
        bool handlesDeletion
    )
        where TBackgroundTask : BackgroundTask
    {
        if (maxBatchCount < 1 || maxTasksPerBatch < 1)
            throw new();
        return serviceCollection
            .AddSingleton(
                new BackgroundTaskSchedulerOptions<TBackgroundTask>(maxBatchCount, maxTasksPerBatch, handlesDeletion))
            .AddSingleton<BackgroundTaskScheduler<TBackgroundTask>>()
            .AddSingleton<IBackgroundTaskScheduler>(sp =>
                sp.GetRequiredService<BackgroundTaskScheduler<TBackgroundTask>>())
            .AddSingleton<IHostedService>(sp =>
                sp.GetRequiredService<BackgroundTaskScheduler<TBackgroundTask>>());
    }
}
