using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NodaTime.Extensions;

namespace StartSch;

public class WordPressHttpClient(HttpClient httpClient)
{
    public async Task<List<WordPressCategory>> GetCategories()
    {
        string url = $"https://vik.hk/wp-json/wp/v2/categories?orderby=id&order=asc&per_page=100&page=1";
        var wordPressCategories = (await httpClient.GetFromJsonAsync<List<WordPressCategory>>(
            url, Utils.JsonSerializerOptions))!;
        if (wordPressCategories.Count > 90) 
            throw new NotImplementedException("TODO: implement category paging");
        return wordPressCategories;
    }

    // https://developer.wordpress.org/rest-api/reference/posts/#list-posts
    public async Task<List<WordPressPost>> GetPostsModifiedAfter(Instant after, CancellationToken cancellationToken)
    {
        Dictionary<int, WordPressPost> results = [];
        int pageCount = 1;
        
        // jfc wordpress
        // default(Instant) == 1970-01-01T00:00:00Z -> 0 -> falsey -> fails validation -> returns HTTP 400
        // default(DateTime) == 0001-01-01... -> -694... -> truthy -> HTTP 200
        // https://core.trac.wordpress.org/browser/tags/6.4/src/wp-includes/rest-api.php#L2226
        if (after == default)
            after = after.Plus(Duration.FromSeconds(1));
        
        for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
        {
            string url = $"https://vik.hk/wp-json/wp/v2/posts?orderby=id&order=asc&per_page=100&page={pageIndex}&modified_after={after.ToDateTimeUtc():O}";
            var response = await httpClient.GetAsync(url, cancellationToken);
            pageCount = int.Parse(response.Headers.GetValues("X-WP-TotalPages").Single());
            var entities = await response.Content.ReadFromJsonAsync<List<WordPressPost>>(cancellationToken);
            entities!.ForEach(e => results[e.Id] = e);
        }
        
        return results.Values.ToList();
    }

    public async Task<HashSet<int>> GetPostIds(CancellationToken cancellationToken)
    {
        HashSet<int> result = [];
        DateTime start = default;
        while (true)
        {
            // this *should* hit the only usable index on the posts table: `KEY type_status_date (post_type,post_status,post_date,ID)`
            // https://github.com/WordPress/WordPress/blob/master/wp-admin/includes/schema.php#L185
            string url = $"https://vik.hk/wp-json/wp/v2/posts?orderby=date&order=asc&per_page=100&after={start:O}&_fields=id,date";
            var response = await httpClient.GetAsync(url, cancellationToken);
            
            List<WordPressId> entities = (await response.Content.ReadFromJsonAsync<List<WordPressId>>(cancellationToken))!;
            result.UnionWith(entities.Select(e => e.Id));
            
            int pageCount = int.Parse(response.Headers.GetValues("X-WP-TotalPages").Single());
            if (pageCount < 2)
                return result;

            start = entities.Max(e => e.Date) - TimeSpan.FromSeconds(10);
        }
    }
}

[UsedImplicitly]
public record WordPressId(int Id, DateTime Date);

[UsedImplicitly]
public record WordPressCategory(
    int Id,
    string Link,
    string Name,
    int Parent,
    int Count
);

[UsedImplicitly]
public record WordPressPost(
    [property: JsonPropertyName("date_gmt"), JsonConverter(typeof(WordPressGmtInstantConverter))]
    Instant DateGmt,
    int Id,
    string Link,
    [property: JsonPropertyName("modified_gmt"), JsonConverter(typeof(WordPressGmtInstantConverter))]
    Instant ModifiedGmt,
    WordPressRendered Title,
    WordPressRendered Content,
    WordPressRendered Excerpt,
    [property: JsonPropertyName("featured_media")]
    int FeaturedMedia,
    List<int> Categories,
    List<int> Tags
);

public record struct WordPressRendered(
    string Rendered
);

public class WordPressGmtInstantConverter : JsonConverter<Instant>
{
    public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc).ToInstant();

    public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
