using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace StartSch.Data;

public sealed record ContentPage(IReadOnlyList<object> Items, string? NextCursor);

public static class ContentPaging
{
    public const int PageSize = 30;

    private const int PostKind = 0;
    private const int EventKind = 1;

    public static async Task<ContentPage> LoadAsync(
        Db db,
        IQueryable<Event> rootEvents,
        IQueryable<Post> rootPosts,
        IQueryable<Post> postSource,
        string? after,
        CancellationToken cancellationToken = default)
    {
        ContentReference? cursor = after == null ? null : ContentReference.Deserialize(after);

        var refsQuery = rootEvents
            .Select(e => new { Date = e.Start ?? e.Created, Kind = EventKind, e.Id })
            .Concat(
                rootPosts.Select(p => new { Date = p.Published ?? p.Created, Kind = PostKind, p.Id })
            );

        if (cursor is { } c)
        {
            refsQuery = refsQuery.Where(r =>
                r.Date < c.Date
                || (
                    r.Date == c.Date && (
                        r.Kind < c.Kind
                        || (
                            r.Kind == c.Kind && r.Id < c.Id
                        )
                    )
                )
            );
        }

        List<ContentReference> rootRefs = (await refsQuery
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Kind)
                .ThenByDescending(r => r.Id)
                .Take(PageSize)
                .ToListAsync(cancellationToken))
            .Select(r => new ContentReference(r.Date, r.Kind, r.Id))
            .ToList();

        List<int> rootEventIds = rootRefs.Where(r => r.Kind == EventKind).Select(r => r.Id).ToList();
        List<int> rootPostIds = rootRefs.Where(r => r.Kind == PostKind).Select(r => r.Id).ToList();

        List<Event> events = rootEventIds.Count == 0
            ? []
            : await db.Events
                .GetEventsAndDescendantEvents(rootEventIds)
                .Include(e => e.EventCategories)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

        List<int> eventIds = events.Select(e => e.Id).ToList();

        List<Post> posts = await postSource
            .Where(p => rootPostIds.Contains(p.Id) || (p.EventId != null && eventIds.Contains(p.EventId.Value)))
            .Include(p => p.PostCategories)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        Dictionary<int, Event> eventsById = events.ToDictionary(e => e.Id);
        Dictionary<int, Post> postsById = posts.ToDictionary(p => p.Id);
        List<object> resultRoots = new(rootRefs.Count);
        resultRoots.AddRange(
            rootRefs.Select(rootRef => (object)(
                rootRef.Kind == EventKind ? eventsById[rootRef.Id] : postsById[rootRef.Id]
            ))
        );

        return new(resultRoots, rootRefs.LastOrDefault()?.Serialize());
    }

    private sealed record ContentReference(Instant Date, int Kind, int Id)
    {
        public static ContentReference Deserialize(string cursor)
        {
            string[] parts = cursor.Split(',');
            if (parts.Length != 3)
                throw new FormatException("Invalid content cursor.");

            return new(
                InstantPattern.ExtendedIso.Parse(parts[0]).GetValueOrThrow(),
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        public string Serialize() =>
            $"{InstantPattern.ExtendedIso.Format(Date)},{Kind.ToString(CultureInfo.InvariantCulture)},{Id.ToString(CultureInfo.InvariantCulture)}";
    }
}
