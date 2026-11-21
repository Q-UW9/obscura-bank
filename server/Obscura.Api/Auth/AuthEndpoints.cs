using System.Security.Claims;
using Obscura.Api.Data;
using Obscura.Api.Security;
using Obscura.Api.Sessions;

namespace Obscura.Api.Auth;

public static class AuthEndpoints
{
    // Extension method:
    // lets Program.cs write app.MapAuthEndpoints()
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync);

        // The current user
        // Anonymous requests get 401 from the authorization middleware
        app.MapGet("/api/me", GetMeAsync).RequireAuthorization();

        return group;
    }

    // 201 on success (no Location, no id)
    // 400 on validation errors
    // 409 when the email is taken
    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, PasswordHasher hasher, UserRepository users
    )
    {
        var errors = RegisterValidator.Validate(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        // Validate guarantees both are present;
        // the compiler cannot see that, hence the '!'
        var email = EmailAddress.Normalize(request.Email!);
        var hash = hasher.Hash(request.Password!);

        var id = await users.CreateAsync(email, hash);
        if (id is null)
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "This email is already registered."
            );

        return Results.StatusCode(StatusCodes.Status201Created);
    }

    // 204 on success (E2 will create the session here)
    // 401 on every failure
    // + SessionStore and HttpContext -> read the incoming cookie and set the new one
    private static async Task<IResult> LoginAsync(
        LoginRequest request, PasswordHasher hasher, UserRepository users,
        SessionStore sessions, HttpContext http)
    {
        var failure = Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Wrong email or password."
        );

        if (!LoginValidator.IsAcceptable(request))
            return failure;

        var user = await users.FindByEmailAsync(EmailAddress.Normalize(request.Email!));

        // Unknown email:
        // verify agains the dummy hash anyways, so both paths cost one
        // Argon2id run
        var passwordOk = hasher.Verify(request.Password!, user?.PasswordHash ?? hasher.DummyHash);
        if (user is null || !passwordOk)
            return failure;

        // Only after a successful Verify:
        // the one moment plaintext is here to rehash
        if (hasher.NeedsRehash(user.PasswordHash))
            await users.UpdatePasswordHashAsync(user.Id, hasher.Hash(request.Password!));

        // Fixation
        // A session that existed before this login (planted or left over) is ended,
        // never reused
        if (SessionToken.TryHash(http.Request.Cookies[SessionAuthenticationHandler.CookieName]) is { } oldHash)
            await sessions.DeleteAsync(oldHash);

        // A brand-new session
        // The cookie carries the token, the database only its hash
        var token = await sessions.CreateAsync(user.Id);
        SessionCookie.Set(http.Response, token);

        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(ClaimsPrincipal user, UserRepository users)
    {
        var profile = await users.FindProfileAsync(user.GetUserId());
        return profile is null ? Results.Unauthorized() : Results.Ok(profile);
    }

    // Ends the session on the server first, then removes the cookie.
    // Always 204, even without a session, so a logout after the session has already
    // expired still clears the browser's cookie.
    private static async Task<IResult> LogoutAsync(HttpContext http, SessionStore sessions)
    {
        if (SessionToken.TryHash(http.Request.Cookies[SessionAuthenticationHandler.CookieName]) is { } tokenHash)
            await sessions.DeleteAsync(tokenHash);

        SessionCookie.Expire(http.Response);
        return Results.NoContent();
    }
}