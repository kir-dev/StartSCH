using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using StartSch.Data;

namespace StartSch.Tests;

[TestClass]
public sealed class ContentPagingTests
{
    [TestMethod]
    public async Task PagesTopLevelItemsWithoutDuplicatesOrGaps()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();

        DbContextOptions<Db> options = new DbContextOptionsBuilder<Db>()
            .UseSqlite(connection, o => o.UseNodaTime())
            .Options;

        await using Db db = new(options);
        await db.Database.EnsureCreatedAsync();

        Page page = new() { Name = "Test page" };
        Category category = new() { Page = page, Name = null };
        page.Categories.Add(category);

        Event rootEvent = new()
        {
            Title = "Root event",
            Start = Instant.FromUtc(2026, 1, 10, 12, 0),
            Created = Instant.FromUtc(2026, 1, 1, 0, 0),
        };
        rootEvent.Categories.Add(category);

        Event childEvent = new()
        {
            Title = "Child event",
            Parent = rootEvent,
            Start = Instant.FromUtc(2026, 1, 11, 12, 0),
            Created = Instant.FromUtc(2026, 1, 2, 0, 0),
        };

        Post childPost = new()
        {
            Title = "Child post",
            Event = rootEvent,
            Published = Instant.FromUtc(2026, 1, 12, 12, 0),
            Created = Instant.FromUtc(2026, 1, 3, 0, 0),
        };

        List<Post> rootPosts = [];
        for (int i = 0; i < 40; i++)
        {
            Post post = new()
            {
                Title = $"Root post {i}",
                Published = Instant.FromUtc(2026, 2, 1, 0, 0).Plus(Duration.FromMinutes(i)),
                Created = Instant.FromUtc(2026, 1, 4, 0, 0).Plus(Duration.FromMinutes(i)),
            };
            post.Categories.Add(category);
            rootPosts.Add(post);
        }

        db.Add(page);
        db.AddRange(rootEvent, childEvent, childPost);
        db.AddRange(rootPosts);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        IQueryable<Event> eventRoots = db.Events
            .Where(e => e.ParentId == null && e.Categories.Any(c => c.Id == category.Id));
        IQueryable<Post> postRoots = db.Posts
            .Where(p => p.EventId == null && p.Categories.Any(c => c.Id == category.Id));

        List<object> allItems = [];
        string? after = null;
        int pageCount = 0;

        do
        {
            ContentPage contentPage = await ContentPaging.LoadAsync(db, eventRoots, postRoots, db.Posts, after);
            allItems.AddRange(contentPage.Items);
            after = contentPage.NextCursor;
            pageCount++;

            if (contentPage.HasMore)
                Assert.AreEqual(ContentPaging.PageSize, contentPage.Items.Count);

            db.ChangeTracker.Clear();
        } while (after != null);

        Assert.AreEqual(41, allItems.Count);
        Assert.AreEqual(2, pageCount);

        HashSet<string> keys = allItems.Select(GetKey).ToHashSet();
        Assert.AreEqual(allItems.Count, keys.Count);

        Event loadedRootEvent = allItems.OfType<Event>().Single(e => e.Id == rootEvent.Id);
        Assert.IsTrue(loadedRootEvent.Children.Any(e => e.Id == childEvent.Id));
        Assert.IsTrue(loadedRootEvent.Posts.Any(p => p.Id == childPost.Id));
    }

    [TestMethod]
    public async Task PagesItemsWithEqualDatesWithoutDuplicatesOrGaps()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();

        DbContextOptions<Db> options = new DbContextOptionsBuilder<Db>()
            .UseSqlite(connection, o => o.UseNodaTime())
            .Options;

        await using Db db = new(options);
        await db.Database.EnsureCreatedAsync();

        Page page = new() { Name = "Test page" };
        Category category = new() { Page = page, Name = null };
        page.Categories.Add(category);

        Instant date = Instant.FromUtc(2026, 3, 1, 12, 0);

        Event rootEvent = new()
        {
            Title = "Root event",
            Start = date,
            Created = date,
        };
        rootEvent.Categories.Add(category);

        List<Post> rootPosts = [];
        for (int i = 0; i < 40; i++)
        {
            Post post = new()
            {
                Title = $"Root post {i}",
                Published = date,
                Created = date,
            };
            post.Categories.Add(category);
            rootPosts.Add(post);
        }

        db.Add(page);
        db.Add(rootEvent);
        db.AddRange(rootPosts);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        IQueryable<Event> eventRoots = db.Events
            .Where(e => e.ParentId == null && e.Categories.Any(c => c.Id == category.Id));
        IQueryable<Post> postRoots = db.Posts
            .Where(p => p.EventId == null && p.Categories.Any(c => c.Id == category.Id));

        List<object> allItems = [];
        string? after = null;
        do
        {
            ContentPage contentPage = await ContentPaging.LoadAsync(db, eventRoots, postRoots, db.Posts, after);
            allItems.AddRange(contentPage.Items);
            after = contentPage.NextCursor;
            db.ChangeTracker.Clear();
        } while (after != null);

        Assert.AreEqual(41, allItems.Count);
        Assert.AreEqual(allItems.Count, allItems.Select(GetKey).ToHashSet().Count);
        Assert.IsInstanceOfType<Event>(allItems[0]);
    }

    private static string GetKey(object item) => item switch
    {
        Event e => $"event{e.Id}",
        Post p => $"post{p.Id}",
        _ => throw new InvalidOperationException(),
    };
}
