using System.Security.Claims;
using System.Text.Json;

namespace StartSch;

public record GroupMembership(int PekId, string Name, List<string> Titles);

public static class GroupMembershipExtensions
{
    public static List<GroupMembership>? GetGroupMemberships(this ClaimsPrincipal claimsPrincipal)
    {
        string? value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "memberships")?.Value;
        return value != null
            ? JsonSerializer.Deserialize<List<GroupMembership>>(value)
            : null;
    }
}