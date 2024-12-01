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

        // update Group.PincerName
        List<Group> groups = await db.Groups.ToListAsync(cancellationToken);
        var existingPincerNames = groups
            .Where(g => g.PincerName != null)
            .Select(g => g.PincerName!)
            .ToHashSet();
        var pincerGroupNames = doc.DocumentNode
            .Descendants("div")
            .First(n => n.HasClass("left-menu"))
            .Descendants("span")
            .Select(n => n.InnerText)
            .Where(s => s.Length >= 4)
            .ToList();
        foreach (string pincerName in pincerGroupNames)
        {
            if (existingPincerNames.Contains(pincerName)) continue;

            List<Group> candidates = groups
                .Where(g =>
                    g.PekName?.RoughlyMatches(pincerName) == true
                    || g.PincerName?.RoughlyMatches(pincerName) == true
                )
                .ToList();
            switch (candidates.Count)
            {
                case > 1:
                    throw new($"Multiple candidates for {pincerName}");
                case 1:
                    candidates[0].PincerName = pincerName;
                    continue;
                default:
                    Group group = new() { PincerName = pincerName };
                    db.Groups.Add(group);
                    groups.Add(group);
                    break;
            }
        }

        // parse openings
        //
        // schpincer pseudo query:
        // SELECT circle.displayName, circle.alias, opening.dateStart, opening.feeling, circle.id
        // WHERE opening.dateEnd > now AND opening.dateEnd < now + week
        // ORDER BY opening.dateStart
        List<OpeningInfo> infos = doc.DocumentNode.ChildNodes
            .Descendants("table")
            .First()
            .ChildNodes
            .Where(n => n.Name == "tr")
            .Select(tr =>
                {
                    var tds = tr.ChildNodes.Where(n => n.Name == "td").ToArray();
                    DateTime startHu = DateTime.ParseExact(
                        tds.First(n => n.HasClass("date")).InnerText,
                        "HH:mm (yy-MM-dd)",
                        CultureInfo.InvariantCulture
                    );
                    return new OpeningInfo(
                        tds.First().ChildNodes.FindFirst("a").InnerText,
                        new(startHu, Utils.HungarianTimeZone.GetUtcOffset(startHu)),
                        tds.First(n => n.HasClass("feeling")).InnerText
                    );
                }
            )
            .ToList();

        List<Group> pincerGroups = groups.Where(g => g.PincerName != null).ToList();
        Dictionary<string, Group> pincerNameToGroup = pincerGroups.ToDictionary(g => g.PincerName!);
        Dictionary<Group, (List<OpeningInfo> Infos, List<Opening> Openings)> dict = pincerGroups.ToDictionary(
            g => g,
            g => (new List<OpeningInfo>(), new List<Opening>())
        );
        foreach (OpeningInfo info in infos)
        {
            Group group = pincerNameToGroup[info.GroupName];
            var entry = dict[group];
            entry.Infos.Add(info);
        }

        var unfinishedOpenings = await db.Openings
            .Include(o => o.Event.Groups)
            .Where(o => !o.Event.EndUtc.HasValue)
            .ToListAsync(cancellationToken);
        foreach (Opening opening in unfinishedOpenings)
        {
            Group group = opening.Event.Groups[0];
            var entry = dict[group];
            entry.Openings.Add(opening);
        }

        foreach (var group in dict)
        {
            UpdateOpenings(group.Key, group.Value.Infos, group.Value.Openings.ToHashSet(), db);
        }

        await db.SaveChangesAsync(cancellationToken);

        return DateTimeOffset.Now.AddMinutes(3);
    }

    // opening update types:
    // - added
    // - cancelled
    // - moved
    // - ended
    // assume only one happens per poll
    private static void UpdateOpenings(
        Group group,
        IEnumerable<OpeningInfo> infos,
        HashSet<Opening> unfinishedOpenings,
        Db db
    )
    {
        DateTime utcNow = DateTime.UtcNow;

        // infos are ordered by start
        foreach (OpeningInfo info in infos)
        {
            Opening? opening = unfinishedOpenings.MinBy(o => (info.Start.UtcDateTime - o.Event.StartUtc).Duration());
            if (opening == null)
            {
                // not yet seen opening, add it to db
                db.Openings.Add(new()
                {
                    Event = new()
                    {
                        Groups = { group },
                        CreatedUtc = utcNow,
                        StartUtc = info.Start.UtcDateTime,
                        Title = info.Title
                    },
                });
            }
            else
            {
                // assume the closest one to be the same opening
                unfinishedOpenings.Remove(opening); // mark it as existing
                opening.Event.Title = info.Title; // update if changed
                opening.Event.StartUtc = info.Start.UtcDateTime;
            }
        }

        foreach (Opening opening in unfinishedOpenings)
        {
            bool hasStarted = opening.Event.StartUtc <= utcNow;
            if (hasStarted)
            {
                // disappeared because it ended
                opening.Event.EndUtc = utcNow;
            }
            else
            {
                // disappeared without starting, probably cancelled
                db.Openings.Remove(opening);
            }
        }
    }

    record OpeningInfo(string GroupName, DateTimeOffset Start, string Title);
}