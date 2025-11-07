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
            Dictionary<int, Event> externalIdToExternalEvent = [];
            DateTime utcNow = DateTime.UtcNow;
            DateTime currentMonth = new(utcNow.Year, utcNow.Month, 1);
            DateTime startMonth = currentMonth.AddMonths(-2);
            DateTime endMonth = currentMonth.AddMonths(7);
            DateOnly firstDate = Utils.GetMondayOfWeekOf(DateOnly.FromDateTime(startMonth));
            DateOnly lastDate = Utils.GetSundayOfWeekOf(
                new DateOnly(
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

                foreach (var dateContainer in html.GetElementsByClassName("calendar_day_events"))
                {
                    var hintDate = DateOnly.ParseExact(
                        dateContainer.Id.RemoveFromStart("calendar_day_events_"), "yyyy-MM-dd");

                    // take every other element, starting from the first (index 0)
                    foreach (var eventContainer in dateContainer.Children.Even())
                    {
                        var a = (IHtmlAnchorElement)eventContainer.FirstElementChild!;
                        int externalId = int.Parse(a.GetAttribute("href").AsSpan(..^1));
                        ref var entry =
                            ref CollectionsMarshal.GetValueRefOrAddDefault(externalIdToExternalEvent, externalId, out bool _);
                        entry ??= new()
                        {
                            ExternalUrl = KthBmeHuModule.Url,
                            ExternalIdInt = externalId,
                            Start = hintDate.ToDateTime(TimeOnly.MinValue).HungarianToUtc(),
                            Title = a.TextContent.Trim(),
                            AllDay = true,
                        };
                        
                        entry.End = hintDate.ToDateTime(Utils.EndOfDay).HungarianToUtc();
                    }
                }
            }

            // remove events that might have dates outside of the retrieved dates
            externalIdToExternalEvent = externalIdToExternalEvent
                .Where(pair =>
                    DateOnly.FromDateTime(pair.Value.Start!.Value.Date) != firstDate &&
                    DateOnly.FromDateTime(pair.Value.End!.Value) != lastDate)
                .ToDictionary();
            
            var externalIds = externalIdToExternalEvent.Keys.ToList();
            Dictionary<int, Event> externalIdToInternalEvent = await db.Events
                .Where(e => e.Categories.Any(c => c.Page == page) && externalIds.Contains(e.ExternalIdInt!.Value))
                .ToDictionaryAsync(e => e.ExternalIdInt!.Value, cancellationToken);
            
            foreach ((int externalId, Event externalEvent) in externalIdToExternalEvent)
            {
                if (externalIdToInternalEvent.TryGetValue(externalId, out Event? internalEvent))
                {
                    internalEvent.Title = externalEvent.Title;
                    internalEvent.DescriptionMarkdown = externalEvent.DescriptionMarkdown;
                    internalEvent.Start = externalEvent.Start;
                    internalEvent.End = externalEvent.End;
                    internalEvent.ExternalUrl = externalEvent.ExternalUrl;

                    // [MIGRATION]
                    internalEvent.AllDay = true;
                }
                else
                {
                    defaultCategory.Events.Add(externalEvent);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (sendNotifications)
            backgroundTaskManager.Notify();
    }
}
