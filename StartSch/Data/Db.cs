using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryInclude> CategoryIncludes => Set<CategoryInclude>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<InterestSubscription> InterestSubscriptions => Set<InterestSubscription>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PincerOpening> PincerOpenings => Set<PincerOpening>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<User> Users => Set<User>();

    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<CategoryInterest> CategoryInterests => Set<CategoryInterest>();
    public DbSet<EventInterest> EventInterests => Set<EventInterest>();
    public DbSet<ShowEventsInCategory> ShowEventsInCategory => Set<ShowEventsInCategory>();
    public DbSet<ShowPostsForEvent> ShowPostsForEvent => Set<ShowPostsForEvent>();
    public DbSet<ShowPostsInCategory> ShowPostsInCategory => Set<ShowPostsInCategory>();
    public DbSet<EmailWhenOrderingStartedInCategory> EmailWhenOrderingStartedInCategory => Set<EmailWhenOrderingStartedInCategory>();
    public DbSet<EmailWhenPostPublishedForEvent> EmailWhenPostPublishedForEvent => Set<EmailWhenPostPublishedForEvent>();
    public DbSet<EmailWhenPostPublishedInCategory> EmailWhenPostPublishedInCategory => Set<EmailWhenPostPublishedInCategory>();
    public DbSet<PushWhenOrderingStartedInCategory> PushWhenOrderingStartedInCategory => Set<PushWhenOrderingStartedInCategory>();
    public DbSet<PushWhenPostPublishedForEvent> PushWhenPostPublishedForEvent => Set<PushWhenPostPublishedForEvent>();
    public DbSet<PushWhenPostPublishedInCategory> PushWhenPostPublishedInCategory => Set<PushWhenPostPublishedInCategory>();
    
    public DbSet<BackgroundTask> BackgroundTasks => Set<BackgroundTask>();
    public DbSet<CreateOrderingStartedNotifications> CreateOrderingStartedNotifications => Set<CreateOrderingStartedNotifications>();
    public DbSet<CreatePostPublishedNotifications> CreatePostPublishedNotifications => Set<CreatePostPublishedNotifications>();
    public DbSet<SendEmail> SendEmails => Set<SendEmail>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<SendPushNotification> SendPushNotifications => Set<SendPushNotification>();
    public DbSet<PushNotificationMessage> PushNotificationMessages => Set<PushNotificationMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // https://learn.microsoft.com/en-us/ef/core/modeling/#applying-all-configurations-in-an-assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#many-to-many-with-navigations-to-and-from-join-entity
        modelBuilder.Entity<User>()
            .HasMany(u => u.Interests)
            .WithMany(i => i.Subscribers)
            .UsingEntity<InterestSubscription>();

        PostgresDateTimeFix.Apply(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        ChangeTracker.DetectChanges();
        try
        {
            ChangeTracker.AutoDetectChangesEnabled = false;

            var utcNow = DateTime.UtcNow;
            
            foreach (var entityEntry in ChangeTracker.Entries<IAutoCreatedUpdated>())
            {
                if (entityEntry.State is EntityState.Added or EntityState.Modified)
                    entityEntry.Entity.Updated = utcNow;
                if (entityEntry.State is EntityState.Added)
                    entityEntry.Entity.Created = utcNow;
            }
            
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}

