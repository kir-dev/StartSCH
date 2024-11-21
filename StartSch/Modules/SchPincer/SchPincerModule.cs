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
        List<OpeningDto> openings = doc.DocumentNode.ChildNodes // openings from now to now + 7 days
            .Descendants("table")
            .First()
            .ChildNodes
            .Where(n => n.Name == "tr")
            .Select(tr =>
                {
                    var tds = tr.ChildNodes.Where(n => n.Name == "td").ToArray();
                    return new OpeningDto(
                        tds.First().ChildNodes.FindFirst("a").InnerText,
                        DateTime.ParseExact(
                            tds.First(n => n.HasClass("date")).InnerText,
                            "HH:mm (yy-MM-dd)", null),
                        tds.First(n => n.HasClass("feeling")).InnerText
                    );
                }
            )
            .ToList();

        DateTime utcNow = DateTime.UtcNow;
        List<Data.Opening> savedUpcomingOpenings = await db.Openings
            .Include(o => o.Group)
            .Where(o => o.StartUtc > utcNow)
            .ToListAsync(cancellationToken);
        HashSet<Data.Opening> seenOpenings = [];
        List<Group> pincerGroups = groups.Where(g => g.PincerName != null).ToList();
        Dictionary<string, Group> pincerNameToGroup = pincerGroups.ToDictionary(g => g.PincerName!);

        // update closest or add new opening
        foreach (OpeningDto dto in openings)
        {
            DateTime startUtc = new DateTimeOffset(
                dto.StartHu,
                Utils.HungarianTimeZone.GetUtcOffset(dto.StartHu)
            ).UtcDateTime;

            Group group = pincerNameToGroup[dto.GroupName];

            Data.Opening dbOpening =
                savedUpcomingOpenings
                    .Where(o => o.Group == group)
                    .MinBy(o => (o.StartUtc - startUtc).Duration())
                ?? db.Openings.Add(new() { Group = group, CreatedAtUtc = utcNow }).Entity;
            dbOpening.StartUtc = startUtc;
            dbOpening.Title = dto.Title;
            seenOpenings.Add(dbOpening);
        }

        // remove cancelled openings
        db.Openings.RemoveRange(savedUpcomingOpenings.Except(seenOpenings));

        await db.SaveChangesAsync(cancellationToken);

        return DateTimeOffset.Now.AddMinutes(3);
    }

    record OpeningDto(string GroupName, DateTime StartHu, string Title);
}