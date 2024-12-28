using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.SchBody;

public class SchBodyPollJob(Db db) : IPollJobExecutor
{
    [UsedImplicitly]
    record PostEntity(
        int Id,
        string Title,
        string Preview,
        string Content,
        DateTime CreatedAt // UTC
    );

    record PostPaginationEntity(
        List<PostEntity> Data
    );

    public async Task Execute(CancellationToken cancellationToken)
    {
        var response = (await new HttpClient().GetFromJsonAsync<PostPaginationEntity>(
            "https://api.body.kir-dev.hu/posts?page=0&page_size=10000",
            cancellationToken))!;
        Dictionary<string, PostEntity> incoming = response.Data.ToDictionary(GetUrl);

        Group group = await db.Groups.FirstOrDefaultAsync(g => g.PekId == 37, cancellationToken)
                      ?? db.Groups.Add(new() { PekId = 37, PekName = "SCHBody" }).Entity;

        Dictionary<string, Post> posts = group.Id != 0
            ? await db.Posts
                .Where(p => p.Groups.Any(g => g.Id == group.Id))
                .ToDictionaryAsync(p => p.Url!, cancellationToken)
            : new();

        UpdatePosts(incoming, posts, db, group);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void UpdatePosts(
        Dictionary<string, PostEntity> incoming,
        Dictionary<string, Post> stored,
        Db db,
        Group group
    )
    {
        var added = incoming.Keys.Except(stored.Keys).ToHashSet();
        var removed = stored.Keys.Except(incoming.Keys).ToHashSet();
        var modified = stored.Keys.Intersect(incoming.Keys).ToHashSet();

        db.Posts.AddRange(added.Select(url =>
        {
            PostEntity source = incoming[url];
            return new Post()
            {
                Title = source.Title.Trim(130),
                Url = url,
                ExcerptMarkdown = source.Preview.Trim(1000),
                ContentMarkdown = source.Content.Trim(20000),
                PublishedUtc = source.CreatedAt,
                Groups = { group },
            };
        }));

        db.Posts.RemoveRange(removed.Select(url => stored[url]));

        foreach (var url in modified)
        {
            Post post = stored[url];
            PostEntity source = incoming[url];
            post.Title = source.Title.Trim(130);
            post.ExcerptMarkdown = source.Preview.Trim(1000);
            post.ContentMarkdown = source.Content.Trim(20000);
            post.PublishedUtc = source.CreatedAt;
        }
    }

    private static string GetUrl(PostEntity post) => $"https://body.sch.bme.hu/#post{post.Id}";
}