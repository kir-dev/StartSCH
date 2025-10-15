using System.Globalization;
using System.Runtime.InteropServices;
using System.ServiceModel.Syndication;
using System.Xml;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io.Network;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.BackgroundTasks;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.KthBmeHu;

public class KthBmeHuPollJob(
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
                        .FirstOrDefaultAsync(p => p.ExternalUrl == KthBmeHuModule.Url, cancellationToken))
                    ?? db.Pages.Add(new()
                    {
                        Name = "KTH",
                        ExternalUrl = KthBmeHuModule.Url,
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

        int updates = await db.SaveChangesAsync(cancellationToken);
        if (updates > 0)
            cache.Remove(InterestService.CacheKey);

        HtmlSanitizer sanitizer = new();
        MinifyMarkupFormatter formatter = new();

        // Posts
        bool sendNotifications = false;
        {
            Stream stream = await httpClient.GetStreamAsync(KthBmeHuModule.RssUrl, cancellationToken);
            XmlReader xmlReader = XmlReader.Create(stream);
            SyndicationFeed syndicationFeed = SyndicationFeed.Load(xmlReader);
            var externalIdToRssItem = syndicationFeed.Items.ToDictionary(i =>
            {
                string url = i.Links[0].Uri.OriginalString;
                var id = url.RemoveFromStart(KthBmeHuModule.CurrentPostUrlPrefix).RemoveFromEnd('/');
                return int.Parse(id);
            });

            List<Post> externalPosts = new(externalIdToRssItem.Count);
            foreach (var (_, syndicationItem) in externalIdToRssItem)
            {
                var url = syndicationItem.Links[0].Uri.OriginalString; // https://www.kth.bme.hu/hirek/aktualis/2333/
                int externalId =
                    int.Parse(url.RemoveFromStart(KthBmeHuModule.CurrentPostUrlPrefix).RemoveFromEnd('/'));

                var detailsContext = BrowsingContext.New(_angleSharpConfig);
                var detailsDocument = await detailsContext.OpenAsync(url, cancellationToken);

                string content = detailsDocument.QuerySelector<IHtmlDivElement>(".news-body")!.InnerHtml;
                string sanitizedContent = sanitizer.Sanitize(content, "", formatter);

                string excerpt = syndicationItem.Summary.Text;
                string sanitizedExcerpt = sanitizer.Sanitize(excerpt, "", formatter);

                externalPosts.Add(new()
                {
                    Title = syndicationItem.Title.Text,
                    ExcerptMarkdown = sanitizedExcerpt,
                    ContentMarkdown = sanitizedContent,
                    ExternalUrl = url,
                    ExternalIdInt = externalId,
                });
            }

            Dictionary<int, Post> externalIdToExternalPost = externalPosts
                .ToDictionary(p => p.ExternalIdInt!.Value);
            var externalIds = externalIdToExternalPost.Keys.ToList();
            Dictionary<int, Post> externalIdToInternalPost = await db.Posts
                .Where(p => p.Categories.Any(c => c.Page == page) && externalIds.Contains(p.ExternalIdInt!.Value))
                .ToDictionaryAsync(p => p.ExternalIdInt!.Value, cancellationToken);

            List<Post> newPosts = [];
            foreach ((int externalId, Post externalPost) in externalIdToExternalPost)
            {
                DateTime publishDate = externalIdToRssItem[externalId].PublishDate.UtcDateTime;

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
                DateTime utcNow = DateTime.UtcNow;
                sendNotifications = true;
                db.CreatePostPublishedNotifications.AddRange(
                    newPosts.Select(p => new CreatePostPublishedNotifications() { Created = utcNow, Post = p })
                );
            }
        }

        // Events
        {
            Dictionary<int, Event> eventMap = [];
            DateTime utcNow = DateTime.UtcNow;
            DateTime currentMonth = new(utcNow.Year, utcNow.Month, 1);
            DateTime startMonth = currentMonth.AddMonths(-2);
            DateTime endMonth = currentMonth.AddMonths(7);
            DateTime firstDate = Utils.GetMondayOfWeekOf(startMonth);
            DateTime lastDate = Utils.GetSundayOfWeekOf(
                new DateTime(
                    endMonth.Year,
                    endMonth.Month,
                    DateTime.DaysInMonth(endMonth.Year, endMonth.Month)
                )
            );

            for (DateTime date = currentMonth.AddMonths(-2);
                 date <= currentMonth.AddMonths(7);
                 date = date.AddMonths(1))
            {
                var response = await httpClient.PostAsync(
                    KthBmeHuModule.CalendarEndpoint,
                    new FormUrlEncodedContent([
                        new("year", date.Year.ToString(CultureInfo.InvariantCulture)),
                        new("month", date.Month.ToString(CultureInfo.InvariantCulture)),
                        new("lang", "hu"),
                    ]),
                    cancellationToken
                );

                var html = await new HtmlParser()
                    .ParseDocumentAsync(await response.Content.ReadAsStreamAsync(cancellationToken));

                foreach (var element in html.GetElementsByClassName("calendar_day_events"))
                {
                    if (element.ChildElementCount != 2)
                        throw new NotImplementedException();
                    var a = (IHtmlAnchorElement)element.FirstElementChild!.FirstElementChild!;
                    var hintDate = DateOnly.ParseExact(
                        element.Id.RemoveFromStart("calendar_day_events_"), "yyyy-MM-dd");
                    int externalId = int.Parse(a.GetAttribute("href").AsSpan(..^1));
                    ref var entry =
                        ref CollectionsMarshal.GetValueRefOrAddDefault(eventMap, externalId, out bool _);
                    entry ??= new()
                    {
                        Title = a.InnerHtml,
                        Start = hintDate.ToDateTime(TimeOnly.MinValue).HungarianToUtc()
                    };
                    entry.End = hintDate.ToDateTime(TimeOnly.MaxValue).HungarianToUtc();
                }
            }

            Console.WriteLine();

            // var browsingContext = BrowsingContext.New(_angleSharpConfig);
            // var eventsDocument = await browsingContext.OpenAsync("https://kth.bme.hu/esemenyek", cancellationToken);
            // var externalEvents = eventsDocument
            //     .QuerySelectorAll(".events-detailed .event")
            //     .Select(eventElement =>
            //     {
            //         var a = eventElement.QuerySelector<IHtmlAnchorElement>("a")!;
            //         int externalId = int.Parse(a.PathName.RemoveFromStart("/esemenyek/").RemoveFromEnd('/'));
            //         string title = a.TextContent;
            //         string dateString = eventElement.QuerySelector(".date")!.TextContent;
            //         (DateOnly start, DateOnly? end) = ParseInterval(dateString);
            //         string description = eventElement.QuerySelector(".description")!.InnerHtml;
            //         return new Event()
            //         {
            //             Title = title,
            //             DescriptionMarkdown = description,
            //             ExternalIdInt = externalId,
            //             ExternalUrl = a.Href,
            //             Start = start
            //                 .ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified)
            //                 .HungarianToUtc(),
            //             End = (end ?? start)
            //                 .ToDateTime(TimeOnly.MaxValue, DateTimeKind.Unspecified)
            //                 .HungarianToUtc(),
            //         };
            //     })
            //     .ToList();
            //
            // Dictionary<int, Event> externalIdToExternalEvent = externalEvents
            //     .ToDictionary(e => e.ExternalIdInt!.Value);
            // var externalIds = externalIdToExternalEvent.Keys.ToList();
            // Dictionary<int, Event> externalIdToInternalEvent = await db.Events
            //     .Where(e => e.Categories.Any(c => c.Page == page) && externalIds.Contains(e.ExternalIdInt!.Value))
            //     .ToDictionaryAsync(e => e.ExternalIdInt!.Value, cancellationToken);
            //
            // foreach ((int externalId, Event externalEvent) in externalIdToExternalEvent)
            // {
            //     if (externalIdToInternalEvent.TryGetValue(externalId, out Event? internalEvent))
            //     {
            //         internalEvent.Title = externalEvent.Title;
            //         internalEvent.DescriptionMarkdown = externalEvent.DescriptionMarkdown;
            //         internalEvent.Start = externalEvent.Start;
            //         internalEvent.End = externalEvent.End;
            //         internalEvent.ExternalUrl = externalEvent.ExternalUrl;
            //     }
            //     else
            //     {
            //         defaultCategory.Events.Add(externalEvent);
            //     }
            // }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (sendNotifications)
            backgroundTaskManager.Notify();
    }

    private record struct EventHint(
        int ExternalId,
        DateOnly Date,
        IHtmlAnchorElement AnchorElement
    );
}
