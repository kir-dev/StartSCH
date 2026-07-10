using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch;

public class DevAuthenticationHandler(
    Db db,
    IOptionsMonitor<DevAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<DevAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        User user = await db.Users.FirstOrDefaultAsync(u => u.AuthSchEmail == "dev@local")
                    ?? db.Users.Add(new() { AuthSchEmail = "dev@local", }).Entity;

        Claim[] claims =
        [
            new(Constants.StartSchUserIdClaim, "1"),
            new(Constants.StartSchPageAdminClaim, "1"),
        ];

        // TODO: add admin role for Pizzasch and show how to switch to a different group
        // When running offline it might be nice to create the Page here instead of expecting the Pincer module to do it

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class DevAuthenticationOptions : AuthenticationSchemeOptions;
