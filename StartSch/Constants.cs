namespace StartSch;

public static class Constants
{
    public const string AuthSchAuthenticationScheme = nameof(AuthSchAuthenticationScheme);
    public const string CookieAuthenticationScheme = nameof(CookieAuthenticationScheme);
    public const string StartSchUserIdClaim = "id";
    public const string StartSchPageAdminClaim = "startsch/page-admin";

    public static bool IsPrivilegedPekTitle(string pekTitle)
    {
        return pekTitle.RoughlyMatches("korvez")
               || pekTitle.RoughlyMatches("admin")
               || pekTitle.Contains("PR");
    }
}
