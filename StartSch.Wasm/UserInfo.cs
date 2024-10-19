using System.Security.Claims;

namespace StartSch.Wasm;

// Add properties to this class and update the server and client
// AuthenticationStateProviders to expose more information about
// the authenticated user to the client.
public sealed class UserInfo
{
    public required string UserId { get; init; }
    public required string? Name { get; init; }
    public required string[]? Roles { get; init; }

    public const string UserIdClaimType = "sub";
    public const string NameClaimType = "name";
    private const string RoleClaimType = "role";

    public static UserInfo FromClaimsPrincipal(ClaimsPrincipal principal) =>
        new()
        {
            UserId = GetClaim(principal, UserIdClaimType)
                     ?? throw new InvalidOperationException("User ID claim not found."),
            Name = GetClaim(principal, NameClaimType),
            Roles = principal
                .FindAll(RoleClaimType)
                .Select(c => c.Value)
                .ToArray(),
        };

    public ClaimsPrincipal ToClaimsPrincipal()
    {
        List<Claim> claims = [new(UserIdClaimType, UserId)];
        if (Roles != null)
            claims.AddRange(Roles.Select(role => new Claim(RoleClaimType, role)));
        if (Name != null)
            claims.Add(new(NameClaimType, Name));
        return new(new ClaimsIdentity(
            claims,
            authenticationType: nameof(UserInfo),
            nameType: NameClaimType,
            roleType: RoleClaimType));
    }

    private static string? GetClaim(ClaimsPrincipal principal, string claimType)
        => principal.FindFirst(claimType)?.Value;
}