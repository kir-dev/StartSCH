using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryInclude> CategoryIncludes => Set<CategoryInclude>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PincerOpening> PincerOpenings => Set<PincerOpening>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<User> Users => Set<User>();
    
    public DbSet<NotificationRequest> NotificationRequests => Set<NotificationRequest>();
    public DbSet<EmailRequest> EmailRequests => Set<EmailRequest>();
    public DbSet<PushRequest> PushRequests => Set<PushRequest>();
    
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OrderingStartedNotification> OrderingStartedNotifications => Set<OrderingStartedNotification>();
    public DbSet<PostNotification> PostNotifications => Set<PostNotification>();

    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<CategoryInterest> CategoryInterests => Set<CategoryInterest>();
    public DbSet<EventInterest> EventInterests => Set<EventInterest>();
    public DbSet<OrderingStartInterest> OrderingStartInterests => Set<OrderingStartInterest>();
    
    public DbSet<InterestSubscription> InterestSubscriptions => Set<InterestSubscription>();
    public DbSet<EmailInterestSubscription> EmailInterestSubscriptions => Set<EmailInterestSubscription>();
    public DbSet<HomepageInterestSubscription> HomepageInterestSubscriptions => Set<HomepageInterestSubscription>();
    public DbSet<PushInterestSubscription> PushInterestSubscriptions => Set<PushInterestSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // https://learn.microsoft.com/en-us/ef/core/modeling/#applying-all-configurations-in-an-assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

