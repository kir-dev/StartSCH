using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<EmailRequest> EmailRequests => Set<EmailRequest>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<OrderingStartedNotification> OrderingStartedNotifications => Set<OrderingStartedNotification>();
    public DbSet<Page> Groups => Set<Page>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRequest> NotificationRequests => Set<NotificationRequest>();
    public DbSet<Opening> Openings => Set<Opening>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostNotification> PostNotifications => Set<PostNotification>();
    public DbSet<PushRequest> PushRequests => Set<PushRequest>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#many-to-many-with-navigations-to-and-from-join-entity
        // modelBuilder.Entity<User>()
        //     .HasMany(e => e.Interests)
        //     .WithMany(e => e.Followers)
        //     .UsingEntity<UserInterest>();
    }
}

