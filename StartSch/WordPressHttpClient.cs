using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace StartSch;

public class WordPressHttpClient(HttpClient httpClient)
{
    public async Task<List<WordPressCategory>> GetCategories()
    {
        string url = $"https://vik.hk/wp-json/wp/v2/categories?orderby=id&order=asc&per_page=100&page=1";
        var wordPressCategories = (await httpClient.GetFromJsonAsync<List<WordPressCategory>>(url))!;
        if (wordPressCategories.Count > 90) 
            throw new NotImplementedException("TODO: implement category paging");
        return wordPressCategories;
    }

    public async Task<List<WordPressPost>> GetPostsModifiedAfter(DateTime after, CancellationToken cancellationToken)
    {
        if (after.Kind != DateTimeKind.Utc) throw new ArgumentException("after must be UTC", nameof(after));

        Dictionary<int, WordPressPost> results = [];
        int pageCount = 1;
        for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
        {
            string url = $"https://vik.hk/wp-json/wp/v2/posts?orderby=id&order=asc&per_page=100&page={pageIndex}&modified_after={after:O}";
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
    [property: JsonPropertyName("date_gmt"), JsonConverter(typeof(WordPressGmtDateTimeConverter))]
    DateTime DateGmt,
    int Id,
    string Link,
    [property: JsonPropertyName("modified_gmt"), JsonConverter(typeof(WordPressGmtDateTimeConverter))]
    DateTime ModifiedGmt,
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

public class WordPressGmtDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
