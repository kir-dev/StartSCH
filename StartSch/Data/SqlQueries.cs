using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public static class SqlQueries
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
               where "Id" = {eventId}
               union
               select "Events"."Id", "Events"."ParentId"
               from "Events"
               join descendants on "Events"."ParentId" = descendants."Id"
            )
            select "Posts".*
            from "Posts"
            join descendants on "Posts"."EventId" = descendants."Id"
            order by "Posts"."CreatedUtc" desc
            """
        );
    }

    public static IQueryable<Event> GetEventsForGroup(this DbSet<Event> events, int groupId)
    {
        // TODO: Fix ForGroup queries
        return events;
        
//         return events.FromSql($"""
//             with recursive descendants as
//             (
//                -- start with all events by a group
//                select "Events".*
//                from "Events"
//                join "EventCategory" on "Events"."Id" = "EventCategory"."EventsId"
//                where "EventCategory"."CategoryId" = {groupId}
//                union
//                -- and add all descendants
//                select "Events".*
//                from "Events"
//                join descendants on "Events"."ParentId" = descendants."Id"
//             )
//             select *
//             from descendants
//             order by "StartUtc" desc
//             """
//         );
    }

    public static dynamic Idk(this Db db)
    {
    }
    public static IQueryable<Post> GetPostsForGroup(this DbSet<Post> posts, int groupId)
    {
        return posts;
//         return posts.FromSql($"""
//             with recursive event_tree as
//             (
//                select "Events"."Id", "Events"."ParentId"
//                from "Events"
//                join "EventGroup" on "Events"."Id" = "EventGroup"."EventsId"
//                where "EventGroup"."GroupsId" = {groupId}
//                union
//                select "Events"."Id", "Events"."ParentId"
//                from "Events"
//                join event_tree on "Events"."ParentId" = event_tree."Id"
//             )
//             
//             select "Posts".*
//             from "Posts"
//             join event_tree on "Posts"."EventId" = event_tree."Id"
//             union
//                 -- posts without an event
//                 select "Posts".*
//                 from "Posts"
//                 join "GroupPost" on "Posts"."Id" = "GroupPost"."PostsId"
//                 where
//                     "GroupPost"."GroupsId" = {groupId}
//                     AND "Posts"."EventId" is null
//             order by "CreatedUtc" desc
//             """
//         );
    }
    
    // 1. load top-level events
    // 2. load sub-events/posts
    // 3. load top-level posts

    public static IQueryable<Post> GetAsd(this DbSet<Post> posts, IEnumerable<int> categoryIds)
    {
        return posts.FromSqlInterpolated(
            $"""
             with recursive
                 categories as (
                     select *
                     from "Categories"
                     where "Id" in ({categoryIds})
                 ),
                 top_level_events as (
                    select distinct "Events".*
                    from "Events"
                    join public."EventCategory" EC on "Events"."Id" = EC."EventId"
                    where
                        EC."CategoryId" in (select categories."Id" from categories)
                        and "ParentId" is null
                 ),
                 event_tree as
                 (
                    select "Events"."Id", "Events"."ParentId"
                    from "Events"
                    join "EventGroup" on "Events"."Id" = "EventGroup"."EventsId"
                    where "EventGroup"."GroupsId" = {groupId}
                    union
                    select "Events"."Id", "Events"."ParentId"
                    from "Events"
                    join event_tree on "Events"."ParentId" = event_tree."Id"
                 )
             select "Posts".*
             from "Posts"
             join event_tree on "Posts"."EventId" = event_tree."Id"
             union
                 -- posts without an event
                 select "Posts".*
                 from "Posts"
                 join "GroupPost" on "Posts"."Id" = "GroupPost"."PostsId"
                 where
                     "GroupPost"."GroupsId" = {groupId}
                     AND "Posts"."EventId" is null
             order by "CreatedUtc" desc
             """
        );
    }
}
