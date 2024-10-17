using System.Security.Claims;

namespace StartSch.Wasm;

// Add properties to this class and update the server and client
// AuthenticationStateProviders to expose more information about
// the authenticated user to the client.
public sealed class UserInfo
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string[] Roles { get; init; }

    public const string UserIdClaimType = "sub";
    public const string NameClaimType = "name";
    private const string RoleClaimType = "role";

    public static UserInfo FromClaimsPrincipal(ClaimsPrincipal principal) =>
        new()
        {
            UserId = GetRequiredClaim(principal, UserIdClaimType),
            Name = GetRequiredClaim(principal, NameClaimType),
            Roles = principal.FindAll(RoleClaimType).Select(c => c.Value)
                .ToArray(),
        };

    public ClaimsPrincipal ToClaimsPrincipal() =>
        new(new ClaimsIdentity(
            Roles.Select(role => new Claim(RoleClaimType, role))
                .Concat([
                    new Claim(UserIdClaimType, UserId),
                    new Claim(NameClaimType, Name),
                ]),
            authenticationType: nameof(UserInfo),
            nameType: NameClaimType,
            roleType: RoleClaimType));

    private static string GetRequiredClaim(ClaimsPrincipal principal,
        string claimType) =>
        principal.FindFirst(claimType)?.Value ??
        throw new InvalidOperationException(
            $"Could not find required '{claimType}' claim.");
}
