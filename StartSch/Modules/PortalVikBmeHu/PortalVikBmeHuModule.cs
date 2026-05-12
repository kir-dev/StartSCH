using System.Collections.Frozen;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Network;
using StartSch.Services;

namespace StartSch.Modules.PortalVikBmeHu;

public class PortalVikBmeHuModule(IHttpClientFactory httpClientFactory) : IModule, IPollJobExecutor
{
    private FrozenDictionary<string, SubjectData> _subjects = FrozenDictionary<string, SubjectData>.Empty;

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<PortalVikBmeHuModule>()
            .SetInterval(TimeSpan.FromDays(1));
    }

    public SubjectData? GetSubject(string subjectId)
    {
        var id = subjectId
            .TryRemoveFromStart("BME")
            .TryRemoveFromEnd("_HU", out _);

        return _subjects
            .GetAlternateLookup<ReadOnlySpan<char>>()
            .TryGetValue(id, out var data)
            ? data
            : null;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient();
        using var browsingContext = BrowsingContext.New(
            Configuration.Default
                .With(new HttpClientRequester(httpClient))
                .WithDefaultLoader()
        );
        using var document = await browsingContext.OpenAsync(
            "https://portal.vik.bme.hu/kepzes/targyak/",
            cancellationToken
        );

        var table = document.QuerySelector<IHtmlTableElement>("table.subject_list");
        if (table is null) return;

        var rows = table.QuerySelectorAll("tr");
        Dictionary<string, SubjectData> dictionary = new(rows.Length);

        foreach (var row in rows)
        {
            if (row.ClassList.Contains("header")) continue;

            var cells = row.QuerySelectorAll("td");
            if (cells.Length < 4) continue;

            var codeAnchor = cells[0].QuerySelector<IHtmlAnchorElement>("a");
            var nameAnchor = cells[1].QuerySelector<IHtmlAnchorElement>("a");
            if (codeAnchor is null || nameAnchor is null) continue;

            string code = codeAnchor.TextContent.Trim();
            string name = nameAnchor.TextContent.Trim();
            if (code.Length == 0 || name.Length == 0) continue;

            string department = cells[2].TextContent.Trim();
            string departmentFull = cells[2].QuerySelector("span")?.GetAttribute("title")?.Trim() ?? "";

            string creditsRaw = cells[3].TextContent.Trim();
            int.TryParse(creditsRaw.Split(' ')[0], out int credits);

            dictionary[code] = new(code, name, department, departmentFull, credits);
        }

        _subjects = dictionary.ToFrozenDictionary();
    }
}

public record SubjectData(string Code, string Name, string Department, string DepartmentFull, int Credits);
