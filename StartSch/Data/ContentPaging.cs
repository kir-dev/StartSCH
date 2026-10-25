using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace StartSch.Data;

public sealed record ContentPage(IReadOnlyList<object> Items, bool HasMore, string? NextCursor);

public static class ContentPaging
{
    public const int PageSize = 30;

    private const int PostKind = 0;
    private const int EventKind = 1;

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

    public static async Task<ContentPage> LoadAsync(
        Db db,
        IQueryable<Event> eventRoots,
        IQueryable<Post> postRoots,
        IQueryable<Post> postSource,
        string? after,
        CancellationToken cancellationToken = default)
    {
        ContentReference? cursor = after == null ? null : ContentReference.Deserialize(after);

        var refsQuery = eventRoots
            .Select(e => new { Date = e.Start ?? e.Created, Kind = EventKind, e.Id })
            .Concat(postRoots.Select(p => new { Date = p.Published ?? p.Created, Kind = PostKind, p.Id }));

        if (cursor is { } c)
        {
            refsQuery = refsQuery.Where(r =>
                r.Date < c.Date
                || (r.Date == c.Date
                    && (r.Kind < c.Kind || (r.Kind == c.Kind && r.Id < c.Id)))
            );
        }

        List<ContentReference> refs = (await refsQuery
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Kind)
                .ThenByDescending(r => r.Id)
                .Take(PageSize + 1)
                .ToListAsync(cancellationToken))
            .Select(r => new ContentReference(r.Date, r.Kind, r.Id))
            .ToList();

        bool hasMore = refs.Count > PageSize;
        if (hasMore)
            refs.RemoveRange(PageSize, refs.Count - PageSize);

        List<int> eventIds = refs.Where(r => r.Kind == EventKind).Select(r => r.Id).ToList();
        List<int> postIds = refs.Where(r => r.Kind == PostKind).Select(r => r.Id).ToList();

        List<Event> events = eventIds.Count == 0
            ? []
            : await db.Events
                .GetSubtrees(eventIds)
                .Include(e => e.EventCategories)
                .ToListAsync(cancellationToken);

        List<int> subtreeEventIds = events.Select(e => e.Id).ToList();

        List<Post> posts = postIds.Count == 0 && subtreeEventIds.Count == 0
            ? []
            : await postSource
                .Where(p => postIds.Contains(p.Id)
                            || (p.EventId != null && subtreeEventIds.Contains(p.EventId.Value)))
                .Include(p => p.PostCategories)
                .ToListAsync(cancellationToken);

        Dictionary<int, Event> eventsById = events.ToDictionary(e => e.Id);
        Dictionary<int, Post> postsById = posts.ToDictionary(p => p.Id);

        List<object> items = new(refs.Count);
        foreach (ContentReference entry in refs)
        {
            items.Add(entry.Kind == EventKind
                ? eventsById[entry.Id]
                : postsById[entry.Id]);
        }

        return new(items, hasMore, hasMore ? refs[^1].Serialize() : null);
    }
}
