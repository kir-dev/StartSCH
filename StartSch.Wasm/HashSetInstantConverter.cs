using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;

namespace StartSch.Wasm;

public class HashSetInstantConverter : JsonConverter<HashSet<Instant>>
{
    public override HashSet<Instant>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<HashSet<Instant>>(ref reader, SharedUtils.JsonSerializerOptionsWebWithNodaTime);

    public override void Write(Utf8JsonWriter writer, HashSet<Instant> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, SharedUtils.JsonSerializerOptionsWebWithNodaTime);
}
