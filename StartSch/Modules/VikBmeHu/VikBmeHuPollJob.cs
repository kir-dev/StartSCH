using System.ServiceModel.Syndication;
using System.Xml;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Network;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NodaTime.Extensions;
using StartSch.BackgroundTasks;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.VikBmeHu;

public class VikBmeHuPollJob(
    HttpClient httpClient,
    Db db,
    IMemoryCache cache,
    BackgroundTaskManager backgroundTaskManager
) : IPollJobExecutor
{
    private readonly AngleSharp.IConfiguration _angleSharpConfig = Configuration.Default
        .With(new HttpClientRequester(httpClient))
        .WithDefaultLoader();

    public async Task Execute(CancellationToken cancellationToken)
    {
        Page page = (await db.Pages
                        .Include(p => p.Categories)
                        .ThenInclude(c => c.Interests)
                        .FirstOrDefaultAsync(p => p.ExternalUrl == VikBmeHuModule.Url, cancellationToken))
                    ?? db.Pages.Add(new()
                    {
                        Name = "VIK",
                        ExternalUrl = VikBmeHuModule.Url,
                        Categories =
                        {
                            new()
                            {
                                Interests =
                                {
                                    new ShowPostsInCategory(),
                                    new EmailWhenPostPublishedInCategory(),
                                    new PushWhenPostPublishedInCategory(),
                                }
                            }
                        }
                    }).Entity;
        Category defaultCategory = page.Categories.Single();

        // [MIGRATION]
        var showEventsInCategoryInterests = defaultCategory.Interests
            .Where(i => i is ShowEventsInCategory).ToList();
        switch (showEventsInCategoryInterests.Count)
        {
            case 0:
                defaultCategory.Interests.Add(new ShowEventsInCategory());
                break;
            // remove entities created by the above because an .Include() was missing previously
            case > 1:
            {
                var survivor = showEventsInCategoryInterests.MinBy(i => i.Id);
                db.Interests.RemoveRange(showEventsInCategoryInterests.Where(x => x != survivor));
                break;
            }
        }

        int updates = await db.SaveChangesAsync(cancellationToken);
        if (updates > 0)
            cache.Remove(InterestService.CacheKey);

        // Posts
        bool sendNotifications = false;
        {
            Stream stream = await httpClient.GetStreamAsync("https://vik.bme.hu/rss", cancellationToken);
            XmlReader xmlReader = XmlReader.Create(stream);
            SyndicationFeed syndicationFeed = SyndicationFeed.Load(xmlReader);
            var externalIdToRssItem = syndicationFeed.Items.ToDictionary(i =>
            {
                string url = i.Links[0].Uri.OriginalString;
                var id = url.RemoveFromStart("https://vik.bme.hu/hir/").RemoveFromEnd('/');
                return int.Parse(id);
            });

            var browsingContext = BrowsingContext.New(_angleSharpConfig);
            var newsDocument = await browsingContext.OpenAsync("https://vik.bme.hu/hirek", cancellationToken);
            var newsItemsTasks = newsDocument
                .QuerySelectorAll(".news-item-list .news-item")
                .Select(async element =>
                {
                    var a = element.QuerySelector<IHtmlAnchorElement>("a")!;

                    string title = a.TextContent;
                    string excerpt = element.QuerySelector(".description")!.InnerHtml;
                    string url = a.Href;
                    string path = a.PathName;
                    var slug = path.RemoveFromStart("/hir/");
                    int dash = slug.IndexOf('-');
                    int externalId = int.Parse(slug[..dash]);

                    var detailsContext = BrowsingContext.New(_angleSharpConfig);
                    var detailsDocument = await detailsContext.OpenAsync(url, cancellationToken);
                    string content = detailsDocument.QuerySelector<IHtmlDivElement>(".page-content")!.InnerHtml;

                    // remove data:image/png;base64,...
                    string sanitizedContent = new HtmlSanitizer().Sanitize(content);

                    return new Post()
                    {
                        Title = title,
                        ExcerptMarkdown = excerpt,
                        ContentMarkdown = sanitizedContent,
                        ExternalUrl = url,
                        ExternalIdInt = externalId,
                    };
                })
                .ToList();
            var externalPosts = await Task.WhenAll(newsItemsTasks);

            Dictionary<int, Post> externalIdToExternalPost = externalPosts
                .ToDictionary(p => p.ExternalIdInt!.Value);
            var externalIds = externalIdToExternalPost.Keys.ToList();
            Dictionary<int, Post> externalIdToInternalPost = await db.Posts
                .Where(p => p.Categories.Any(c => c.Page == page) && externalIds.Contains(p.ExternalIdInt!.Value))
                .ToDictionaryAsync(p => p.ExternalIdInt!.Value, cancellationToken);

            List<Post> newPosts = [];
            foreach ((int externalId, Post externalPost) in externalIdToExternalPost)
            {
                Instant publishDate = externalIdToRssItem[externalId].PublishDate.ToInstant();

                if (externalIdToInternalPost.TryGetValue(externalId, out Post? internalPost))
                {
                    internalPost.Title = externalPost.Title;
                    internalPost.ExcerptMarkdown = externalPost.ExcerptMarkdown;
                    internalPost.ContentMarkdown = externalPost.ContentMarkdown;
                    internalPost.ExternalUrl = externalPost.ExternalUrl;
                }
                else
                {
                    internalPost = externalPost;
                    defaultCategory.Posts.Add(externalPost);
                    newPosts.Add(internalPost);

                    internalPost.Created = publishDate;
                }

                internalPost.Updated = publishDate;
                internalPost.Published = publishDate;
            }

            if (newPosts.Count is 1 or 2 or 3)
            {
                Instant currentInstant = SystemClock.Instance.GetCurrentInstant();
                sendNotifications = true;
                db.CreatePostPublishedNotifications.AddRange(
                    newPosts.Select(p => new CreatePostPublishedNotifications() { Created = currentInstant, Post = p })
                );
            }
        }

        // Events
        {
            var browsingContext = BrowsingContext.New(_angleSharpConfig);
            var eventsDocument = await browsingContext.OpenAsync("https://vik.bme.hu/esemenyek", cancellationToken);
            var externalEvents = eventsDocument
                .QuerySelectorAll(".events-detailed .event")
                .Select(eventElement =>
                {
                    var a = eventElement.QuerySelector<IHtmlAnchorElement>("a")!;
                    int externalId = int.Parse(a.PathName.RemoveFromStart("/esemenyek/").RemoveFromEnd('/'));
                    string title = a.TextContent;
                    string dateString = eventElement.QuerySelector(".date")!.TextContent;
                    (LocalDate start, LocalDate? end) = ParseInterval(dateString);
                    string description = eventElement.QuerySelector(".description")!.InnerHtml;
                    string sanitizedDescription = new HtmlSanitizer()
                        .Sanitize(description, "", new MinifyMarkupFormatter());
                    return new Event()
                    {
                        Title = title,
                        DescriptionMarkdown = sanitizedDescription,
                        ExternalIdInt = externalId,
                        ExternalUrl = a.Href,
                        Start = start
                            .AtMidnight()
                            .InZoneLeniently(Utils.HungarianTimeZone)
                            .ToInstant(),
                        End = (end ?? start)
                            .At(Utils.EndOfDay)
                            .InZoneLeniently(Utils.HungarianTimeZone)
                            .ToInstant(),
                        AllDay = true,
                    };
                })
                .ToList();

            Dictionary<int, Event> externalIdToExternalEvent = externalEvents
                .ToDictionary(e => e.ExternalIdInt!.Value);
            var externalIds = externalIdToExternalEvent.Keys.ToList();
            Dictionary<int, Event> externalIdToInternalEvent = await db.Events
                .Where(e => e.Categories.Any(c => c.Page == page) && externalIds.Contains(e.ExternalIdInt!.Value))
                .ToDictionaryAsync(e => e.ExternalIdInt!.Value, cancellationToken);

            // [MIGRATION]
            // If there are any non-all-day events, mark all events in the DB as all-day
            if (externalIdToInternalEvent.Values.FirstOrDefault() is { AllDay: false })
                await db.Events
                    .Where(e => e.Categories.Any(c => c.Page == page))
                    .ExecuteUpdateAsync(x => x.SetProperty(e => e.AllDay, true), cancellationToken);

            foreach ((int externalId, Event externalEvent) in externalIdToExternalEvent)
            {
                if (externalIdToInternalEvent.TryGetValue(externalId, out Event? internalEvent))
                {
                    internalEvent.Title = externalEvent.Title;
                    internalEvent.DescriptionMarkdown = externalEvent.DescriptionMarkdown;
                    internalEvent.Start = externalEvent.Start;
                    internalEvent.End = externalEvent.End;
                    internalEvent.ExternalUrl = externalEvent.ExternalUrl;
                }
                else
                {
                    defaultCategory.Events.Add(externalEvent);
                }
            }
        }
        
        db.SetCreatedAndUpdatedTimestamps(e => e switch
        {
            Event => TimestampUpdateFlags.CreatedUpdated,
            _ => TimestampUpdateFlags.None,
        });

        await db.SaveChangesAsync(cancellationToken);

        if (sendNotifications)
            backgroundTaskManager.Notify();
    }

    private static (LocalDate Start, LocalDate? End) ParseInterval(ReadOnlySpan<char> s)
    {
        int dash = s.IndexOf('–');
        if (dash != -1)
            return (ParseDate(s[..dash]), ParseDate(s[(dash + 1)..]));
        return (ParseDate(s), null);
    }

    private static LocalDate ParseDate(ReadOnlySpan<char> s)
    {
        s = s.Trim();

        Span<char> s2 = stackalloc char[s.Length];
        s.CopyTo(s2);
        if (s2[^3] == ' ')
            s2[^3] = '0';

        return DateOnly.ParseExact(s2, "yyyy. MMMM dd.", Utils.HungarianCulture).ToLocalDate();
    }
}
