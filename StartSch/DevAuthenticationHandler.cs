using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch;

public class DevAuthenticationOptions : AuthenticationSchemeOptions;

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
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-1"),
            new Claim(ClaimTypes.Name, "dev@local"),
            new Claim(ClaimTypes.Role, Constants.StartSchPageAdminClaim), // whatever roles you need locally
        };
        
        // TODO: add admin role for Pizzasch and show how to switch to a different group
        // When running offline it might be nice to create the Page here instead of expecting the Pincer module to do it

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
