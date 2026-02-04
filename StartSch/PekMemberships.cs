using System.Security.Claims;
using System.Text.Json;

namespace StartSch;

public record GroupMembership(int PekId, string Name, List<string> Titles);

public static class GroupMembershipExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public IEnumerable<GroupMembership> GetGroupMemberships()
        {
            string? json = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "memberships")?.Value;
            if (json == null) return [];
            return JsonSerializer.Deserialize<IEnumerable<GroupMembership>>(json) ?? [];
        }

        public IEnumerable<GroupMembership> GetAdminMemberships()
        {
            return claimsPrincipal
                .GetGroupMemberships()
                .Where(g => g.Titles.Any(Constants.IsPrivilegedPekTitle));
        }
    }
}
