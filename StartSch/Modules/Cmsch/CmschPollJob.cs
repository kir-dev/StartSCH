using System.Text.Json.Serialization;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.BackgroundTasks;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.Cmsch;

public class CmschPollJob(
    HttpClient httpClient,
    Db db,
    IMemoryCache cache,
    BackgroundTaskManager backgroundTaskManager
) : IPollJobExecutor<string>
{
    public async Task Execute(string frontendUrl, CancellationToken cancellationToken)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(25);
        Stream indexHtmlStream = await httpClient.GetStreamAsync(frontendUrl, cancellationToken)
            .HandleHttpExceptions(true);
        HtmlDocument indexHtml = new();
        indexHtml.Load(indexHtmlStream);
        var head = indexHtml.DocumentNode.Descendants("head").First().ChildNodes;
        var manifestUrl = head
            .Single(n => n.GetAttributeValue("rel", null) == "manifest")
            .GetAttributeValue("href", null);
        const string manifestPath = "/manifest/manifest.json";
        if (!manifestUrl.EndsWith(manifestPath)) throw new();
        var backendUrl = manifestUrl[..^(manifestPath.Length)]; // https://api.example.sch.bme.hu

        var app = await httpClient
            .GetFromJsonAsync<AppResponse>($"{backendUrl}/api/app", cancellationToken)
            .HandleHttpExceptions();

        string host = new Uri(frontendUrl).Host;
        string? headTitle = head.Single(n => n.Name == "title").InnerText.IfNotEmpty();
        string? appSiteName = app!.Components.App.SiteName.IfNotEmpty();
        string? countdownMessage = app.Components.Countdown?.Title.IfNotEmpty();
        string eventTitle = appSiteName ?? headTitle ?? countdownMessage ?? host;

        Page page = (await db.Pages
                        .Include(p => p.Categories)
                        .FirstOrDefaultAsync(p => p.ExternalUrl == frontendUrl, cancellationToken))
                    ?? db.Pages.Add(new()
                    {
                        ExternalUrl = frontendUrl,
                        Categories =
                        {
                            new()
                            {
                                Interests =
                                {
                                    new EmailWhenPostPublishedInCategory(),
                                    new PushWhenPostPublishedInCategory(),
                                    new ShowEventsInCategory(),
                                    new ShowPostsInCategory()
                                },
                            },
                        }
                    }).Entity;

        page.Name = eventTitle;

        int rowsAffected = await db.SaveChangesAsync(cancellationToken);
        if (rowsAffected > 0)
            cache.Remove(InterestService.CacheKey);

        Category defaultCategory = page.Categories.Single();

        Event? currentEvent = await db.Events
            .Where(e => e.Categories.Contains(defaultCategory) && e.Parent == null)
            .OrderByDescending(e => e.Start)
            .FirstOrDefaultAsync(cancellationToken);

        // assume a new event after 2.5 months of no activity
        if (currentEvent != null)
        {
            Instant latestPost = await db.Posts
                .OrderByDescending(p => p.Updated)
                .Select(p => p.Updated)
                .FirstOrDefaultAsync(cancellationToken);
            Instant latestEvent = await db.Events
                .OrderByDescending(p => p.Updated)
                .Select(p => p.Updated)
                .FirstOrDefaultAsync(cancellationToken);
            Instant? latestUpdate = latestPost != default && latestEvent != default
                ? (latestPost > latestEvent ? latestPost : latestEvent)
                : (latestPost != default ? latestPost : latestEvent);
            if (SystemClock.Instance.GetCurrentInstant() - latestUpdate > Duration.FromDays(75))
                currentEvent = null;
        }

        if (currentEvent == null)
        {
            currentEvent = new()
            {
                Categories = { defaultCategory },
                Title = eventTitle,
            };
            defaultCategory.Events.Add(currentEvent);
        }
        else
            currentEvent.Title = eventTitle;

        currentEvent.ExternalUrl = page.ExternalUrl;

        if (app.Components.Countdown is { Enabled: true, TimeToCountTo: { } timeToCountTo })
            currentEvent.Start = timeToCountTo;

        if (app.Components.Event is { } eventComponent)
        {
            EventsView eventsView = (await httpClient
                .GetFromJsonAsync<EventsView>($"{backendUrl}/api/events", cancellationToken)
                .HandleHttpExceptions())!;

            if (eventComponent.EnableDetailedView)
            {
                await Task.WhenAll(eventsView.AllEvents
                    .Where(e => !string.IsNullOrWhiteSpace(e.Url))
                    .Select(async e =>
                        {
                            var response = (await httpClient.GetFromJsonAsync<SingleEventView>(
                                    $"{backendUrl}/api/events/{e.Url}", cancellationToken
                                ).HandleHttpExceptions())!
                                .Event;
                            e.Description = response.Description;
                            e.ExtraButtonTitle = response.ExtraButtonTitle;
                            e.ExtraButtonUrl = response.ExtraButtonUrl;
                            e.FullImageUrl = response.FullImageUrl;
                            e.OgTitle = response.OgTitle;
                            e.OgImage = response.OgImage;
                            e.OgDescription = response.OgDescription;
                        }
                    )
                );
            }

            // [MIGRATION] previously cmsch stuff was identified using urls, remove those
            await db.Events
                .Where(e => e.Parent == currentEvent && e.ExternalIdInt == null)
                .ExecuteDeleteAsync(cancellationToken);
            Dictionary<int, Event> externalIdToSubEvent = await db.Events
                .Where(e => e.Parent == currentEvent)
                .ToDictionaryAsync(e => e.ExternalIdInt!.Value, cancellationToken);
            Dictionary<int, EventEntity> externalIdToExternalEvent = eventsView.AllEvents
                .ToDictionary(e => e.Id);

            foreach ((int externalId, EventEntity externalEvent) in externalIdToExternalEvent)
            {
                if (externalIdToSubEvent.Remove(externalId, out Event? internalEvent))
                {
                    internalEvent.Title = externalEvent.Title;
                }
                else
                {
                    internalEvent = new()
                    {
                        Title = externalEvent.Title,
                        Categories = { defaultCategory },
                        Parent = currentEvent,
                        ExternalIdInt = externalId,
                        ExternalUrl = GetAbsoluteUrl(externalEvent),
                    };
                    currentEvent.Children.Add(internalEvent);
                }

                internalEvent.Start = externalEvent.TimestampStart;
                internalEvent.End = externalEvent.TimestampEnd;
                internalEvent.DescriptionMarkdown = string.IsNullOrWhiteSpace(externalEvent.Description)
                    ? externalEvent.PreviewDescription
                    : externalEvent.Description;
            }

            db.Events.RemoveRange(externalIdToSubEvent.Values);

            currentEvent.Start = currentEvent.Children.Min(e => e.Start);

            string GetAbsoluteUrl(EventEntity e) => frontendUrl + (
                !string.IsNullOrWhiteSpace(e.Url)
                    ? $"/event/{e.Url}"
                    : $"/event#post{e.Id}"
            );
        }

        bool backgroundTasksUpdated = false;
        if (app.Components.News is { } newsComponent)
        {
            NewsView newsView = (await httpClient
                .GetFromJsonAsync<NewsView>($"{backendUrl}/api/news", cancellationToken)
                .HandleHttpExceptions())!;

            if (newsComponent.ShowDetails)
            {
                await Task.WhenAll(newsView.News
                    .Where(n => !string.IsNullOrWhiteSpace(n.Url))
                    .Select(async n =>
                        {
                            var response = await httpClient.GetFromJsonAsync<NewsEntity>(
                                $"{backendUrl}/api/news/{n.Url}", cancellationToken);
                            n.Content = response!.Content;
                            n.OgTitle = response.OgTitle;
                            n.OgImage = response.OgImage;
                            n.OgDescription = response.OgDescription;
                            n.Highlighted = response.Highlighted;
                        }
                    )
                );
            }
            
            // [MIGRATION] previously cmsch stuff was identified using urls, remove those
            await db.Posts
                .Where(p => p.Event == currentEvent && p.ExternalIdInt == null)
                .ExecuteDeleteAsync(cancellationToken);
            
            Dictionary<int, Post> externalIdToPost = await db.Posts
                .Where(p => p.Event == currentEvent)
                .ToDictionaryAsync(p => p.ExternalIdInt!.Value, cancellationToken);
            Dictionary<int, NewsEntity> externalIdToExternalPost = newsView.News
                .ToDictionary(n => n.Id);
                
            DateTime utcNow = DateTime.UtcNow;

            List<Post> newPosts = [];
            foreach ((int externalId, NewsEntity response) in externalIdToExternalPost)
            {
                if (!externalIdToPost.Remove(externalId, out Post? post))
                {
                    post = new()
                    {
                        Categories = { defaultCategory },
                        Event = currentEvent,
                        Published = utcNow,
                        ExternalIdInt = externalId,
                        ExternalUrl = GetAbsoluteUrl(response),
                    };
                    currentEvent.Posts.Add(post);
                    newPosts.Add(post);
                }

                post.Title = response.Title;
                post.ExcerptMarkdown = response.BriefContent;
                post.ContentMarkdown = response.Content;
            }

            if (newPosts.Count is 1 or 2 or 3)
            {
                db.CreatePostPublishedNotifications.AddRange(
                    newPosts.Select(p => new CreatePostPublishedNotifications(){Created = utcNow, Post = p})
                );
                backgroundTasksUpdated = true;
            }

            db.Posts.RemoveRange(externalIdToPost.Values);

            string GetAbsoluteUrl(NewsEntity n) => frontendUrl + (
                !string.IsNullOrWhiteSpace(n.Url)
                    ? $"/news/{n.Url}"
                    : $"/news#post{n.Id}"
            );
        }

        await db.SaveChangesAsync(cancellationToken);
        if (backgroundTasksUpdated)
            backgroundTaskManager.Notify();
    }

    record AppResponse(
        ComponentsResponse Components
    );

    record ComponentsResponse(
        AppComponentResponse App,
        CountdownComponentResponse? Countdown,
        EventComponentResponse? Event,
        NewsComponentResponse? News
    );

    record AppComponentResponse(
        string? SiteName
    );

    record CountdownComponentResponse(
        bool Enabled,
        string Title,
        [property: JsonConverter(typeof(UnixTimeSecondsInstantJsonConverter))]
        Instant? TimeToCountTo
    );

    record EventComponentResponse(
        bool EnableDetailedView
    );

    record NewsComponentResponse(
        bool ShowDetails
    );

    record EventsView(
        List<EventEntity> AllEvents
    );

    record SingleEventView(
        EventEntity Event
    );

    class EventEntity
    {
        public required int Id { get; set; }
        public required string Url { get; set; }
        public required string Title { get; set; }
        public required string Category { get; set; }

        [property: JsonConverter(typeof(UnixTimeSecondsInstantJsonConverter))]
        public required Instant? TimestampStart { get; set; }

        [property: JsonConverter(typeof(UnixTimeSecondsInstantJsonConverter))]
        public required Instant? TimestampEnd { get; set; }

        public required string Place { get; set; }

        // Preview
        public string? PreviewDescription { get; set; }
        public string? PreviewImageUrl { get; set; }

        // FullDetails
        public string? Description { get; set; }
        public string? ExtraButtonTitle { get; set; }
        public string? ExtraButtonUrl { get; set; }
        public string? FullImageUrl { get; set; }
        public string? OgTitle { get; set; }
        public string? OgImage { get; set; }
        public string? OgDescription { get; set; }
    }

    record NewsView(
        List<NewsEntity> News
    );

    class NewsEntity
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string ImageUrl { get; set; }

        [property: JsonConverter(typeof(UnixTimeSecondsInstantJsonConverter))]
        public required DateTime? Timestamp { get; set; }

        // Preview
        public string? Url { get; set; }
        public string? BriefContent { get; set; }
        public bool? Highlighted { get; set; }

        // Details
        public string? Content { get; set; }
        public string? OgTitle { get; set; }
        public string? OgImage { get; set; }
        public string? OgDescription { get; set; }
    }
}
