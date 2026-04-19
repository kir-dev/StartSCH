using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;

namespace StartSch.Wasm;

// There is no global way to tell Blazor how to serialize NodaTime types. This is a workaround for that.
public class HashSetInstantConverter : JsonConverter<HashSet<Instant>>
{
    public override HashSet<Instant>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<HashSet<Instant>>(ref reader, SharedUtils.JsonSerializerOptionsWebWithNodaTime);

    public override void Write(Utf8JsonWriter writer, HashSet<Instant> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, SharedUtils.JsonSerializerOptionsWebWithNodaTime);
}
