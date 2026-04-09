using System.Security.Cryptography;
using System.Text;
using NodaTime;

namespace StartSch.Wasm;

public static class SharedUtils
{
    // from https://stackoverflow.com/a/73126261
    public static string ComputeSha256(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static DateTimeZone HungarianTimeZone { get; } = DateTimeZoneProviders.Tzdb["Europe/Budapest"];
}
