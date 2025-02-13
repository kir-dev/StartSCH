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
        return events.FromSql($"""
            with recursive descendants as
            (
               -- start with all events by a group
               select "Events".*
               from "Events"
               join "EventGroup" on "Events"."Id" = "EventGroup"."EventsId"
               where "EventGroup"."GroupsId" = {groupId}
               union
               -- and add all descendants
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

    public static IQueryable<Post> GetPostsForGroup(this DbSet<Post> posts, int groupId)
    {
        return posts.FromSql($"""
            with recursive descendants as
            (
               select "Events"."Id", "Events"."ParentId"
               from "Events"
               join "EventGroup" on "Events"."Id" = "EventGroup"."EventsId"
               where "EventGroup"."GroupsId" = {groupId}
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
}
