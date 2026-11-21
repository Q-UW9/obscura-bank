namespace Obscura.Api.Sessions;

// The session cookie comes from the E2 decision
// One place for its flags, so setting and deleting always match.
public static class SessionCookie
{
    // No Expires/Max-Age: a browser-session cookie
    // The server enforces the real timeouts
    private static CookieOptions Options() => new()
    {
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };

    public static void Set(HttpResponse response, string token) =>
        response.Cookies.Append(SessionAuthenticationHandler.CookieName, token, Options());

    // Sends the cookie again with an expiry in the past, so the browser drops it
    public static void Expire(HttpResponse response) =>
        response.Cookies.Delete(SessionAuthenticationHandler.CookieName, Options());
}