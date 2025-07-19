using System.Text.Json.Serialization;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.Cmsch;

public class CmschPollJob(HttpClient httpClient, Db db) : IPollJobExecutor<string>
{
    public async Task Execute(string frontendUrl, CancellationToken cancellationToken)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        Stream indexHtmlStream = await httpClient.GetStreamAsync(frontendUrl, cancellationToken).HandleHttpExceptions();
        HtmlDocument indexHtml = new();
        indexHtml.Load(indexHtmlStream);
        var manifestUrl = indexHtml.DocumentNode
            .Descendants("head")
            .First()
            .ChildNodes
            .Single(n => n.GetAttributeValue("rel", null) == "manifest")
            .GetAttributeValue("href", null);
        const string manifestPath = "/manifest/manifest.json";
        if (!manifestUrl.EndsWith(manifestPath)) throw new();
        var backendUrl = manifestUrl[..^(manifestPath.Length)]; // https://api.example.sch.bme.hu

        var app = await httpClient
            .GetFromJsonAsync<AppResponse>($"{backendUrl}/api/app", cancellationToken)
            .HandleHttpExceptions();

        string eventTitle = app!.Components.App.SiteName
                            ?? app.Components.Countdown?.Title
                            ?? new Uri(frontendUrl).Host;

        Page page = (await db.Pages
                        .Include(p => p.Categories)
                        .FirstOrDefaultAsync(p => p.Url == frontendUrl, cancellationToken))
                    ?? db.Pages.Add(new() { Url = frontendUrl }).Entity;
        if (page.Id == 0)
        {
            page.Categories.Add(
                new()
                {
                    Interests =
                    {
                        new EmailWhenPostPublishedInCategory(),
                        new PushWhenPostPublishedInCategory(),
                        new ShowEventsInCategory(),
                        new ShowPostsInCategory()
                    },
                    Page = page,
                }
            );
        }

        page.Name = eventTitle;

        Category defaultCategory = page.Categories.Single();

        if (defaultCategory.Id != 0)
            await db.Events.Include(e => e.EventCategories).Where(e => e.Categories.Contains(defaultCategory))
                .LoadAsync(cancellationToken);

        Event? currentEvent = defaultCategory.Events.FirstOrDefault();

        // assume a new event after 2.5 months of no activity
        if (currentEvent != null)
        {
            DateTime latestPost = await db.Posts
                .OrderByDescending(p => p.Updated)
                .Select(p => p.Updated)
                .FirstOrDefaultAsync(cancellationToken);
            DateTime latestEvent = await db.Events
                .OrderByDescending(p => p.Updated)
                .Select(p => p.Updated)
                .FirstOrDefaultAsync(cancellationToken);
            DateTime? latestUpdate = latestPost != default && latestEvent != default
                ? (latestPost > latestEvent ? latestPost : latestEvent)
                : (latestPost != default ? latestPost : latestEvent);
            if (DateTime.UtcNow - latestUpdate > TimeSpan.FromDays(75))
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

            // get an ID for the currentEvent that can be used in URLs
            await db.SaveChangesAsync(cancellationToken);
        }

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

            if (currentEvent.Id != 0)
                await db.Events.Where(e => e.Parent == currentEvent).LoadAsync(cancellationToken);
            Dictionary<string, Event> urlToSubEvent = currentEvent.Children
                .DistinctBy(e => e.Url)
                .ToDictionary(e => e.Url!);
            Dictionary<string, EventEntity> urlToResponseItem = eventsView.AllEvents
                .DistinctBy(GetAbsoluteUrl)
                .ToDictionary(GetAbsoluteUrl);

            foreach ((string url, EventEntity response) in urlToResponseItem)
            {
                if (urlToSubEvent.Remove(url, out Event? ev))
                {
                    ev.Title = response.Title;
                }
                else
                {
                    ev = new()
                    {
                        Title = response.Title,
                        Categories = { defaultCategory },
                        Parent = currentEvent,
                        Url = url,
                    };
                    currentEvent.Children.Add(ev);
                }

                ev.Start = response.TimestampStart;
                ev.End = response.TimestampEnd;
                ev.DescriptionMarkdown = string.IsNullOrWhiteSpace(response.Description)
                    ? response.PreviewDescription
                    : response.Description;
            }

            db.Events.RemoveRange(urlToSubEvent.Values);

            currentEvent.Start = currentEvent.Children.Select(e => e.Start).Min();

            string GetAbsoluteUrl(EventEntity e) => frontendUrl + (
                !string.IsNullOrWhiteSpace(e.Url)
                    ? $"/event/{e.Url}?startsch={currentEvent.Id}"
                    : $"/event?id={e.Id}&startsch={currentEvent.Id}"
            );
        }

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

            if (currentEvent.Id != 0)
                await db.Posts.Where(p => p.Event == currentEvent).LoadAsync(cancellationToken);

            Dictionary<string, Post> urlToPost = currentEvent.Posts.ToDictionary(p => p.ExternalUrl!);
            Dictionary<string, NewsEntity> urlToResponse = newsView.News.ToDictionary(GetAbsoluteUrl);

            DateTime utcNow = DateTime.UtcNow;

            foreach ((string url, NewsEntity response) in urlToResponse)
            {
                if (urlToPost.Remove(url, out Post? post))
                {
                }
                else
                {
                    post = new()
                    {
                        Categories = { defaultCategory },
                        Event = currentEvent,
                        Published = utcNow,
                        ExternalUrl = url,
                    };
                    currentEvent.Posts.Add(post);
                }

                post.Title = response.Title;
                post.ExcerptMarkdown = response.BriefContent;
                post.ContentMarkdown = response.Content;
            }

            string GetAbsoluteUrl(NewsEntity n) => frontendUrl + (
                !string.IsNullOrWhiteSpace(n.Url)
                    ? $"/news/{n.Url}?startsch={currentEvent.Id}"
                    : $"/news?id={n.Id}&startsch={currentEvent.Id}"
            );
        }

        await db.SaveChangesAsync(cancellationToken);
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
        [property: JsonConverter(typeof(UnixTimeSecondsDateTimeJsonConverter))]
        DateTime? TimeToCountTo // UTC
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

        [property: JsonConverter(typeof(UnixTimeSecondsDateTimeJsonConverter))]
        public required DateTime? TimestampStart { get; set; }

        [property: JsonConverter(typeof(UnixTimeSecondsDateTimeJsonConverter))]
        public required DateTime? TimestampEnd { get; set; }

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

        [property: JsonConverter(typeof(UnixTimeSecondsDateTimeJsonConverter))]
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
