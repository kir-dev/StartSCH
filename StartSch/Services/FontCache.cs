using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;

namespace StartSch.Services;

// https://developers.google.com/fonts/docs/css2#forming_api_urls

public class FontCache
{
    private static List<FontRequest> FontRequests =>
    [
        new()
        {
            Families =
            [
                new()
                {
                    Name = "DM Serif Text",
                    ParameterNames = "wght",
                    ParameterVariations =
                    [
                        "300",
                        "400",
                    ],
                },
                new ()
                {
                    Name = "PT Serif",
                    ParameterNames = "ital,wght",
                    ParameterVariations =
                    [
                        "0,400..700",
                        "1,400..700",
                    ],
                }
            ],
            Display = "swap",
        },
        new()
        {
            Families =
            [
                new()
                {
                    Name = "Material Symbols Outlined",
                    ParameterNames = "opsz,wght,FILL,GRAD",
                    ParameterVariations = ["20..48,100..700,0..1,-50..200"],
                },
            ],
            Display = "block",
            IconNames =
            [
                "admin_panel_settings",
                "arrow_forward",
                "bug_report",
                "calendar_add_on",
                "chat",
                "chat_add_on",
                "chat_paste_go",
                "chevron_right",
            "close",
                "code",
                "edit",
                "event",
                "explore",
                "help",
                "history",
                "home",
                "lightbulb",
                "login",
                "logout",
                "mail",
                "menu",
                "mobile_chat",
                "more_vert",
                "newspaper",
                "notification_important",
                "notifications",
                "open_in_browser",
                "open_in_full",
                "open_in_new",
                "restaurant",
                "send_to_mobile",
                "settings",
                "shopping_cart",
                "shopping_cart_off",
            ],
        },
    ];

    class FontRequest
    {
        public required List<FontFamily> Families { get; init; }
        public string? Display { get; init; }
        public string? Text { get; init; }

        /// Must be in alphabetical order for the Google Fonts API to accept it
        public List<string>? IconNames { get; init; }
    }

    class FontFamily
    {
        public required string Name { get; init; }
        public required string ParameterNames { get; init; }
        public required List<string> ParameterVariations { get; init; }
    }

    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    public FontCache(IMemoryCache cache, HttpClient httpClient)
    {
        _cache = cache;
        _httpClient = httpClient;

#if DEBUG
        HotReloadHandler.ClearCacheEvent += types =>
        {
            if (types?.Any(t => t == typeof(FontCache)) ?? false)
                cache.Remove(CacheKey);
        };
#endif
    }

    private const string CacheKey = "FontStyles";

    public async Task<MarkupString> GetFontStyles()
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            (string Css, TimeSpan? MaxAge)[] results = await Task.WhenAll(FontRequests.Select(async fontRequest =>
            {
                Uri uri = GetUri(fontRequest);
                HttpRequestMessage request = new(HttpMethod.Get, uri);

                // without this, we get 21k lines back for the symbols request.
                // the version will have to be incremented occasionally
                request.Headers.UserAgent.Add(new("Firefox", "141"));

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string s = await response.Content.ReadAsStringAsync();
                return (s, response.Headers.CacheControl?.MaxAge);
            }));

            string css = string.Join('\n', results.Select(x => x.Css));
            string minified = Minify(css);

            entry.AbsoluteExpirationRelativeToNow = results.Min(x => x.MaxAge);

            return new MarkupString(minified);
        }))!;
    }

    private static Uri GetUri(FontRequest fontRequest)
    {
        StringBuilder query = new("?");
        query.AppendJoin(
            '&',
            fontRequest.Families.Select(f =>
                $"family={f.Name}:{f.ParameterNames}@{string.Join(';', f.ParameterVariations)}")
        );
        if (fontRequest.Display != null)
            query.Append($"&display={fontRequest.Display}");
        if (fontRequest.IconNames != null)
            query.Append($"&icon_names={string.Join(',', fontRequest.IconNames)}");
        if (fontRequest.Text != null)
            query.Append($"&text={fontRequest.Text}");

        UriBuilder uriBuilder = new("https://fonts.googleapis.com/css2");
        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri;
    }

    private static string Minify(string css)
    {
        // https://github.com/AngleSharp/AngleSharp.Css/issues/187
        //css = new CssParser().ParseStyleSheet(css).Minify();

        return css;
    }
}
