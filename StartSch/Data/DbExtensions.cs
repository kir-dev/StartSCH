using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public static class DbExtensions
{
    public static IQueryable<Event> GetDescendants(this DbSet<Event> events, int parentId)
    {
        return events.FromSql($"""
            with recursive descendants as
            (
               select "Events".*
               from "Events"
               where "ParentId" = {parentId}
               union
               select "Events".*
               from "Events"
               join descendants on "Events"."ParentId" = descendants."Id"
            )
            select *
            from descendants
            order by "StartUtc" desc
            """
        );
    }

    public static IQueryable<Post> GetPostsForEvent(this DbSet<Post> posts, int eventId)
    {
        return posts.FromSql($"""
            with recursive descendants as
            (
               select "Events"."Id", "Events"."ParentId"
               from "Events"
               where "ParentId" = {eventId}
               union
               select "Events"."Id", "Events"."ParentId"
               from "Events"
               inner join
                   descendants on "Events"."ParentId" = descendants."Id"
            )
            select "Posts".*
            from "Posts"
            join descendants on "Posts"."EventId" = descendants."Id"
            order by "Posts"."CreatedUtc" desc
            """
        );
    }
}
