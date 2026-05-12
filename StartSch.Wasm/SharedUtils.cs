using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NodaTime.Serialization.SystemTextJson;

namespace StartSch.Wasm;

public static class SharedUtils
{
    public static CultureInfo HungarianCulture { get; } = new("hu-HU");
    public static DateTimeZone HungarianTimeZone { get; } = DateTimeZoneProviders.Tzdb["Europe/Budapest"];
    
    // from https://stackoverflow.com/a/73126261
    public static string ComputeSha256(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static JsonSerializerOptions JsonSerializerOptionsWebWithNodaTime { get; }
        = new JsonSerializerOptions(JsonSerializerOptions.Web)
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    static SharedUtils()
    {
        JsonSerializerOptionsWebWithNodaTime.MakeReadOnly();
    }

    public static string RgbToCssColorString(uint x)
    {
        x &= 0x00FF_FFFF; // zero out first byte
        return $"#{x:X6}";
    }

    // by NodaTime.IsoDayOfWeek
    private static ImmutableArray<string> DaysOfTheWeekNames { get; } =
    [
        null!, "hétfő", "kedd", "szerda", "csütörtök", "péntek", "szombat", "vasárnap",
    ];
    
    public static string GetHungarianName(IsoDayOfWeek dayOfWeek) => DaysOfTheWeekNames[(int)dayOfWeek];
}
