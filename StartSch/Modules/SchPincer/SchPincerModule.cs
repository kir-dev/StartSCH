using System.Globalization;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Modules.SchPincer;

public class SchPincerModule(IDbContextFactory<Db> dbFactory) : IModule
{
    public string Id => "pincer";
    public IEnumerable<Post> Posts => [];
    public IEnumerable<Event> Events => [];
    public IEnumerable<Instance> Instances => [new("https://schpincer.sch.bme.hu", "SCH-Pincér")];

    public IEnumerable<Opening> Openings { get; private set; } = [];

    public IEnumerable<TagGroup> Tags =>
    [
        new("nyitás", null, [
            new("Lángosch", "lang"),
            new("Magyarosch", "hungry"),
        ]),
        new("push", null, [
            new("pincér", "Pincer", [
                new("hírek"),
                new("rendelés"),
                new("nyitás"),
                new("lángosch", "lang", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
                new("magyarosch", "hungry", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
            ]),
            new("esemény"),
        ]),
        new("email", null, [
            new("pincér", "Pincer", [
                new("hírek"),
                new("rendelés"),
                new("nyitás"),
                new("lángosch", "lang", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
                new("magyarosch", "hungry", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
            ]),
            new("esemény"),
        ]),
    ];

    public IEnumerable<Func<CancellationToken, Task<DateTimeOffset>>> CronJobs =>
    [
        GetUpcomingOpenings
    ];

    private async Task<DateTimeOffset> GetUpcomingOpenings(CancellationToken stoppingToken)
    {
        HtmlDocument doc = new();
        doc.Load(await new HttpClient().GetStreamAsync("https://schpincer.sch.bme.hu", stoppingToken));
        List<OpeningDto> openings = doc.DocumentNode.ChildNodes.Descendants("table").First().ChildNodes
            .Where(n => n.Name == "tr").Select(tr =>
                {
                    var tds = tr.ChildNodes.Where(n => n.Name == "td").ToArray();
                    var a = tds[0].ChildNodes.First(n => n.Name == "a");
                    return new OpeningDto(
                        a.Attributes["href"].Value.Split('/')[2],
                        a.InnerText,
                        DateTime.ParseExact(tds[2].InnerText, "HH:mm (yy-MM-dd)", null),
                        tds[3].InnerText
                    );
                }
            ).ToList();
        Openings = openings
            .Select(o =>
                new Opening(
                    o.CircleName, o.Title, null,
                    new DateTimeOffset(
                        o.StartDateCet,
                        TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest")
                            .GetUtcOffset(o.StartDateCet)
                    ).UtcDateTime,
                    null))
            .ToList();

        // var candidates = await db.
        return DateTimeOffset.Now.AddMinutes(3);
    }

    record OpeningDto(string CircleIdString, string CircleName, DateTime StartDateCet, string Title);
}