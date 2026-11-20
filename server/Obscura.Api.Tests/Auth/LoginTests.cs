using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Obscura.Api.Security;
using Obscura.Api.Tests.Infrastructure;
namespace Obscura.Api.Tests.Auth;

[Collection(DatabaseCollection.Name)]
public sealed class LoginTests : IAsyncLifetime
{
    private const string ValidPassword = "correct horse battery staple";

    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;
    private readonly PasswordHasher _hasher = new(Argon2Parameters.Default);

    public LoginTests(DatabaseFixture db)
    {
        _db = db;
        _client = db.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _db.ResetDataAsync();
        await RegisterAsync("alice@example.com", ValidPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_CorrectPassword_Returns204()
    {
        var responses = await LoginAsync("alice@example.com", ValidPassword);

        Assert.Equal(HttpStatusCode.NoContent, responses.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsNormalized_Returns204()
    {
        var response = await LoginAsync("   ALICE@Example.com ", ValidPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Attack = user enumeration:
    // every kind of failure must look exactly the same
    [Theory]
    [InlineData("alice@example.com", "wrong password here")]         // wrong password
    [InlineData("nobody@example.com", ValidPassword)]                // unknown email
    [InlineData("alice@example.com", "Correct Horse Battery Staple")] // case matters
    [InlineData("alice@example.com", "")]
    [InlineData("alice@example.com", null)]
    [InlineData(null, ValidPassword)]
    [InlineData("", ValidPassword)]
    public async Task Login_AnyFailure_ReturnsTheSameGeneric401(string? email, string? password)
    {
        var response = await LoginAsync(email, password);

        await AssertGenericFailureAsync(response);
    }

    [Fact]
    public async Task Login_PasswordOverMaximum_ReturnsTheSameGeneric401()
    {
        var response = await LoginAsync("alice@example.com", new string('a', PasswordPolicy.MaxLength + 1));

        await AssertGenericFailureAsync(response);
    }

    // Attack = timing enumeration:
    // an unknown email must cost the same Argon2id run as a wrong password
    // Without the dummy hash, the unknown-email path is ~50 times
    // faster, so half is a generous margin
    [Fact]
    public async Task Login_UnknownEmail_TakesAsLongAsWrongPassword()
    {
        // first call pays extra cost
        await LoginAsync("alice@example.com", "warm-up request");

        var wrongPassword = await TimeAsync(() => LoginAsync("alice@example.com", "wrong password here"));
        var unknownEmail = await TimeAsync(() => LoginAsync("nobody@example.com", "wrong passwrod here"));

        Assert.True(unknownEmail >= wrongPassword / 2,
            $"unknown email took {unknownEmail.TotalMilliseconds} ms, wrong password {wrongPassword.TotalMilliseconds} ms");
    }

    // Decision: the 15-char minimum is a registration rule
    // A user with an older, shorter password
    // (inserted directly because registration would refuse it)
    // can still log in
    [Fact]
    public async Task Login_PasswordBelowRegistrationMinimum_StillWorks()
    {
        await InsertUserAsync("old@example.com", _hasher.Hash("tenletters"));

        var response = await LoginAsync("old@example.com", "tenletters");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Rehash:
    // a failed login leaves an old hash alone
    // a successful one upgrades it to current parameters
    [Fact]
    public async Task Login_OldHash_IsUpgradedOnlyAfterSuccess()
    {
        var oldHash = new PasswordHasher(new Argon2Parameters(8_192, 1, 1)).Hash(ValidPassword);
        await InsertUserAsync("bob@example.com", oldHash);

        await LoginAsync("bob@example.com", "wrong password here");
        Assert.Equal(oldHash, await ReadHashAsync("bob@example.com"));

        await LoginAsync("bob@example.com", ValidPassword);
        var upgraded = await ReadHashAsync("bob@example.com");
        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", upgraded);
        Assert.True(_hasher.Verify(ValidPassword, upgraded));
    }

    private static async Task AssertGenericFailureAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Wrong email or password.", problem!.Title);
        Assert.Null(problem.Detail);
    }

    private static async Task<TimeSpan> TimeAsync(Func<Task> action)
    {
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 3; i++)
            await action();
        return watch.Elapsed;
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, password });

    private Task<HttpResponseMessage> LoginAsync(string? email, string? password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task InsertUserAsync(string email, string passwordHash)
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync(
            "INSERT INTO users (email, password_hash) VALUES (@email, @passwordHash)",
            new { email, passwordHash });
    }

    private async Task<string> ReadHashAsync(string email)
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<string>(
            "SELECT password_hash FROM users WHERE email = @email", new { email });
    }
}