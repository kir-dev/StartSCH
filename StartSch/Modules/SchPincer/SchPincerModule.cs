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
            new("Lángosch", ""),
            new("Magyarosch"),
            new("PizzáSCH"),
            new("ReggeliSCH"),
        ]),
        new("push", null, [
            new("pincér", "Pincer", [
                new("hírek", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
                new("rendelés", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
                new("nyitás", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
            ]),
            new("esemény"),
        ]),
        new("email", null, [
            new("pincér", "Pincer", [
                new("hírek", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
                new("rendelés", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
                new("nyitás", "", [
                    new("Lángosch", ""),
                    new("Magyarosch"),
                    new("PizzáSCH"),
                    new("ReggeliSCH"),
                ]),
            ]),
            new("esemény"),
        ]),
    ];

    public IEnumerable<Func<CancellationToken, Task<DateTimeOffset>>> CronJobs =>
    [
        GetUpcomingOpenings
    ];

    private async Task<DateTimeOffset> GetUpcomingOpenings(CancellationToken cancellationToken)
    {
        HtmlDocument doc = new();
        doc.Load(await new HttpClient().GetStreamAsync("https://schpincer.sch.bme.hu", cancellationToken));

        await using Db db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // update groups
        List<Group> groups = await db.Groups.ToListAsync(cancellationToken);
        var existing = groups
            .Where(g => g.PincerName != null)
            .Select(g => g.PincerName!)
            .ToHashSet();
        var groupNames = doc.DocumentNode
            .Descendants("div")
            .First(n => n.HasClass("left-menu"))
            .Descendants("span")
            .Select(n => n.InnerText)
            .Where(s => s.Length >= 4)
            .ToList();
        foreach (string groupName in groupNames)
        {
            if (existing.Contains(groupName)) continue;

            List<Group> candidates = groups
                .Where(g =>
                    g.PekName?.RoughlyMatches(groupName) == true
                    || g.PincerName?.RoughlyMatches(groupName) == true
                )
                .ToList();
            switch (candidates.Count)
            {
                case > 1:
                    throw new($"Multiple candidates for {groupName}");
                case 1:
                    candidates[0].PincerName = groupName;
                    continue;
                default:
                    db.Groups.Add(new() { PincerName = groupName });
                    break;
            }
        }

        // parse openings
        List<OpeningDto> openings = doc.DocumentNode.ChildNodes
            .Descendants("table")
            .First()
            .ChildNodes
            .Where(n => n.Name == "tr")
            .Select(tr =>
                {
                    var tds = tr.ChildNodes.Where(n => n.Name == "td").ToArray();
                    var a = tds[0].ChildNodes.First(n => n.Name == "a");
                    return new OpeningDto(
                        a.InnerText,
                        DateTime.ParseExact(tds[2].InnerText, "HH:mm (yy-MM-dd)", null),
                        tds[3].InnerText
                    );
                }
            )
            .ToList();
        Openings = openings
            .Select(o =>
                new Opening(
                    o.GroupName, o.Title, null,
                    new DateTimeOffset(
                        o.StartDateCet,
                        TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest")
                            .GetUtcOffset(o.StartDateCet)
                    ).UtcDateTime,
                    null))
            .ToList();

        await db.SaveChangesAsync(cancellationToken);

        return DateTimeOffset.Now.AddMinutes(3);
    }

    record OpeningDto(string GroupName, DateTime StartDateCet, string Title);
}