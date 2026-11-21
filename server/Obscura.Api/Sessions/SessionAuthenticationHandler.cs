using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Obscura.Api.Sessions;

// My own authentication scheme (E2):
// __Host-session cookie -> SHA-256 -> sessions table -> ClaimsPrincipal
// No cookie means an anonymous request, not an error.
// Challenge = 401
// Forbidden = 403
public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Session";
    public const string CookieName = "__Host-session";

    private readonly SessionStore _sessions;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SessionStore sessions)
        : base(options, logger, encoder)
    {
        _sessions = sessions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var token))
            return AuthenticateResult.NoResult();
        
        var tokenHash = SessionToken.TryHash(token);
        if (tokenHash is null)
            return AuthenticateResult.Fail("Malformed session cookie.");

        var user = await _sessions.FindValidAsync(tokenHash);
        if (user is null)
            return AuthenticateResult.Fail("Unknown or expired session.");

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Role, user.Role),
            ],
            SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}