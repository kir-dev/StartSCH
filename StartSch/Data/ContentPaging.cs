using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace StartSch.Data;

/// <summary>
/// A page of top-level content items (events and posts).
/// </summary>
/// <param name="Items">The top-level items of this page, in display order.</param>
/// <param name="HasMore">Whether there are items after this page.</param>
/// <param name="NextCursor">The cursor to pass as <c>?after=</c> for the next page.</param>
public sealed record ContentPage(IReadOnlyList<object> Items, bool HasMore, string? NextCursor);

/// <summary>
/// Keyset pagination for the event/post trees shown on page and event pages.
///
/// Only the top-level items are paginated. The complete subtree of every event on the page is loaded
/// in a single recursive query, so rendering the page never needs to query per item.
/// </summary>
public static class ContentPaging
{
    public const int PageSize = 30;

    private const int PostKind = 0;
    private const int EventKind = 1;

    private sealed record ContentRef(Instant Date, int Kind, int Id);

    /// <summary>
    /// Loads a page of content.
    /// </summary>
    /// <param name="eventRoots">The top-level events that may appear on the page.</param>
    /// <param name="postRoots">The top-level posts that may appear on the page.</param>
    /// <param name="postSource">
    /// The posts that may be shown anywhere in the page, including inside event subtrees.
    /// This must be at least as permissive as <paramref name="postRoots"/>.
    /// </param>
    /// <param name="after">The cursor returned by the previous page, or <c>null</c> for the first page.</param>
    public static async Task<ContentPage> LoadAsync(
        Db db,
        IQueryable<Event> eventRoots,
        IQueryable<Post> postRoots,
        IQueryable<Post> postSource,
        string? after,
        CancellationToken cancellationToken = default)
    {
        ContentRef? cursor = after == null ? null : ParseCursor(after);

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

        // Fetch one extra item to find out whether another page exists without running a count query.
        List<ContentRef> refs = (await refsQuery
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Kind)
                .ThenByDescending(r => r.Id)
                .Take(PageSize + 1)
                .ToListAsync(cancellationToken))
            .Select(r => new ContentRef(r.Date, r.Kind, r.Id))
            .ToList();

        bool hasMore = refs.Count > PageSize;
        if (hasMore)
            refs.RemoveRange(PageSize, refs.Count - PageSize);

        List<int> eventIds = refs.Where(r => r.Kind == EventKind).Select(r => r.Id).ToList();
        List<int> postIds = refs.Where(r => r.Kind == PostKind).Select(r => r.Id).ToList();

        // One recursive query loads the root events and all of their descendants.
        List<Event> events = eventIds.Count == 0
            ? []
            : await db.Events
                .GetSubtrees(eventIds)
                .Include(e => e.EventCategories)
                .ToListAsync(cancellationToken);

        List<int> subtreeEventIds = events.Select(e => e.Id).ToList();

        // One query loads the root posts and every post inside the event subtrees.
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
        foreach (ContentRef entry in refs)
        {
            items.Add(entry.Kind == EventKind
                ? eventsById[entry.Id]
                : postsById[entry.Id]);
        }

        return new(items, hasMore, hasMore ? FormatCursor(refs[^1]) : null);
    }

    private static ContentRef ParseCursor(string cursor)
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

    private static string FormatCursor(ContentRef entry) =>
        $"{InstantPattern.ExtendedIso.Format(entry.Date)},{entry.Kind.ToString(CultureInfo.InvariantCulture)},{entry.Id.ToString(CultureInfo.InvariantCulture)}";
}
