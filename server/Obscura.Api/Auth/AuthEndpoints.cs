using Obscura.Api.Data;
using Obscura.Api.Security;

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
    private static async Task<IResult> LoginAsync(
        LoginRequest request, PasswordHasher hasher, UserRepository users)
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

        return Results.NoContent();
    }
}