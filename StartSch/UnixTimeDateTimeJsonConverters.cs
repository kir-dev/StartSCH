using System.Text.Json;
using System.Text.Json.Serialization;

namespace StartSch;

public class UnixTimeMillisecondsInstantJsonConverter : JsonConverter<Instant?>
{
    public override Instant? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long unixMilliseconds = reader.GetInt64();
        if (unixMilliseconds == 0)
            return null;
        return Instant.FromUnixTimeMilliseconds(unixMilliseconds);
    }

    public override void Write(Utf8JsonWriter writer, Instant? value, JsonSerializerOptions options) => throw new();
}

public class UnixTimeSecondsInstantJsonConverter : JsonConverter<Instant?>
{
    public override Instant? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long unixSeconds = reader.GetInt64();
        if (unixSeconds == 0)
            return null;
        return Instant.FromUnixTimeSeconds(unixSeconds);
    }

    public override void Write(Utf8JsonWriter writer, Instant? value, JsonSerializerOptions options) => throw new();
}
