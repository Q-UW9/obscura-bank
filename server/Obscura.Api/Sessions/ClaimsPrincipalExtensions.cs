using System.Globalization;
using System.Security.Claims;

namespace Obscura.Api.Sessions;

public static class ClaimsPrincipalExtensions
{
    // The logged-in user's id
    // Only call this on endpoints with RequireAuthorization():
    // there the session handler has always set the NameIdentifier claim
    public static long GetUserId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
}