using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace StartSch.Data.Queries;

public sealed record ArticlePage(IReadOnlyList<IArticle> Roots, string? NextCursor);

public sealed record RootRef(Instant Date, int Kind, int Id, bool HasMore);

/// Given a SQL root-refs query, page through the roots in order.
/// Event descendant trees are always loaded in full. A page contains at least PageSize articles
/// (unless there are fewer roots left), plus whatever the last root's tree adds on top.
public static class ArticlePaging
{
    public const int PageSize = 30;

    private const int PostKind = 0;
    private const int EventKind = 1;

    /// <param name="rootRefsSql">
    /// A SQL query returning "Id", "Date" and "Kind" for the already filtered root events/posts,
    /// without a trailing semicolon.
    /// </param>
    /// Use inside a Snapshot transaction
    public static async Task<ArticlePage> LoadAsync(
        Db db,
        string rootRefsSql,
        IQueryable<Post> postsTable,
        string? after,
        CancellationToken cancellationToken = default)
    {
        ArticleReference? cursor = after == null ? null : ArticleReference.Deserialize(after);

        string afterPredicate = cursor is null
            ? "1=1"
            : $"""
               ("Date" < '{Format(cursor.Date)}'
                OR ("Date" = '{Format(cursor.Date)}'
                    AND ("Kind" < {cursor.Kind}
                        OR ("Kind" = {cursor.Kind} AND "Id" < {cursor.Id}))))
               """;

        // The candidate roots are limited to PageSize + 1 because every root contributes at least one
        // article. The walk stops after the root that reaches PageSize, so only the prefix is counted.
        string sql = $"""
            with recursive candidate_roots as
            (
                select r.*
                from (
                    {rootRefsSql}
                ) r
                where {afterPredicate}
                order by r."Date" desc, r."Kind" desc, r."Id" desc
                limit {PageSize + 1}
            ),
            ordered_roots as
            (
                select c.*,
                       row_number() over (order by c."Date" desc, c."Kind" desc, c."Id" desc) as "Rn"
                from candidate_roots c
            ),
            walk as
            (
                select o.*, {CountSql("o")} as "Total"
                from ordered_roots o
                where o."Rn" = 1

                union all

                select o.*, w."Total" + {CountSql("o")}
                from walk w
                join ordered_roots o on o."Rn" = w."Rn" + 1
                where w."Total" < {PageSize}
            )
            select "Date", "Kind", "Id",
                   (select count(*) from ordered_roots) > (select count(*) from walk) as "HasMore"
            from walk
            order by "Date" desc, "Kind" desc, "Id" desc
            """;

        List<RootRef> roots = await db.Database
            .SqlQueryRaw<RootRef>(sql)
            .ToListAsync(cancellationToken);

        bool hasMore = roots.Count > 0 && roots[0].HasMore;
        List<ArticleReference> rootRefs = roots
            .Select(r => new ArticleReference(r.Date, r.Kind, r.Id))
            .ToList();

        List<int> rootEventIds = rootRefs.Where(r => r.Kind == EventKind).Select(r => r.Id).ToList();
        List<int> rootPostIds = rootRefs.Where(r => r.Kind == PostKind).Select(r => r.Id).ToList();

        List<Event> allEvents = rootEventIds.Count == 0
            ? []
            : await db.Events
                .GetEventsAndDescendantEvents(rootEventIds)
                .Include(e => e.EventCategories)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

        List<int> allEventIds = allEvents.Select(e => e.Id).ToList();

        List<Post> posts = await postsTable
            .Where(p => rootPostIds.Contains(p.Id) || (p.EventId != null && allEventIds.Contains(p.EventId.Value)))
            .Include(p => p.PostCategories)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // this way we keep the ordering of rootRefs
        Dictionary<int, Event> eventsById = allEvents.ToDictionary(e => e.Id);
        Dictionary<int, Post> postsById = posts.ToDictionary(p => p.Id);
        List<IArticle> resultRoots = new(rootRefs.Count);
        resultRoots.AddRange(
            rootRefs.Select(rootRef => (IArticle)(
                rootRef.Kind == EventKind ? eventsById[rootRef.Id] : postsById[rootRef.Id]
            ))
        );

        return new(resultRoots, hasMore ? rootRefs[^1].Serialize() : null);
    }

    private static string CountSql(string rootAlias) => $"""
        (
            with recursive tree as
            (
                select {rootAlias}."Id" as "Id", {rootAlias}."Kind" as "Kind"
                union
                select e."Id", 1
                from "Events" e
                join tree t on t."Kind" = 1 and e."ParentId" = t."Id"
            )
            select (select count(*) from tree)
                 + (select count(*) from "Posts" p
                    join tree t on p."EventId" = t."Id"
                    where t."Kind" = 1)
        )
        """;

    private static string Format(Instant instant) => InstantPattern.ExtendedIso.Format(instant);

    private sealed record ArticleReference(Instant Date, int Kind, int Id)
    {
        public static ArticleReference Deserialize(string cursor)
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
