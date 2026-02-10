using System.Collections.ObjectModel;

namespace StartSch;

public static class Constants
{
    public const string AuthSchAuthenticationScheme = nameof(AuthSchAuthenticationScheme);
    public const string CookieAuthenticationScheme = nameof(CookieAuthenticationScheme);
    public const string StartSchUserIdClaim = "id";
    public const string StartSchPageAdminClaim = "startsch/page-admin";
    
    public static readonly ReadOnlyCollection<string> TrustedPekTitles =
    [
        "Adminisztrátor",
        "körvezető",
        "körvezető helyettes",
        "PR menedzser",
    ];

    public static bool IsTrustedPekTitle(string pekTitle)
    {
        return TrustedPekTitles.Contains(pekTitle, StringComparer.InvariantCultureIgnoreCase);
    }
}
