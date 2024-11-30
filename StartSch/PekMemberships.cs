using System.Security.Claims;
using System.Text.Json;

namespace StartSch;

public record GroupMembership(int PekId, string Name, List<string> Titles);

public static class GroupMembershipExtensions
{
    public static IEnumerable<GroupMembership> GetGroupMemberships(this ClaimsPrincipal claimsPrincipal)
    {
        string? json = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "memberships")?.Value;
        if (json == null) return [];
        return JsonSerializer.Deserialize<IEnumerable<GroupMembership>>(json) ?? [];
    }

    public static IEnumerable<GroupMembership> GetAdminMemberships(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal
            .GetGroupMemberships()
            .Where(g =>
                g.Titles.Any(t =>
                    t.RoughlyMatches("korvez")
                    || t.RoughlyMatches("admin")
                    || t.Contains("PR")));
    }
}