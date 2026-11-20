using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Obscura.Api.Security;
using Obscura.Api.Tests.Infrastructure;

namespace Obscura.Api.Tests.Auth;

[Collection(DatabaseCollection.Name)]
public sealed class RegisterTests : IAsyncLifetime
{
    private const string ValidPassword = "correct horse battery staple";

    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;

    public RegisterTests(DatabaseFixture db)
    {
        _db = db;
        _client = db.Factory.CreateClient();
    }

    public Task InitializeAsync() => _db.ResetDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ValidRequest_Returns201_AndStoresNormalizedEmail()
    {
        var response = await RegisterAsync("  Alice@Example.com ", ValidPassword);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());

        var (email, _) = await ReadOnlyUserAsync();
        Assert.Equal("alice@example.com", email);
    }

    // Attack = stolen database
    // the stored value is an Argon2id PHC string, never the password
    [Fact]
    public async Task Register_StoresArgon2idHash_NeverThePassword()
    {
        await RegisterAsync("alice@example.com", ValidPassword);

        var (_, passwordHash) = await ReadOnlyUserAsync();
        Assert.StartsWith("$argon2id$v=19$", passwordHash);
        Assert.DoesNotContain(ValidPassword, passwordHash);
        Assert.True(new PasswordHasher(Argon2Parameters.Default).Verify(ValidPassword, passwordHash));
    }

    [Fact]
    public async Task Register_TakenEmail_OtherSpelling_Returns409Problem()
    {
        await RegisterAsync("alice@example.com", ValidPassword);

        var response = await RegisterAsync("ALICE@example.com", "another long passphrase");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(1L, await CountUsersAsync());
    }

    // Attack = check-then-insert race
    // parallel sign-ups with one email create exactly one account
    [Fact]
    public async Task Register_SameEmailInParallel_CreatesExactlyOneUser()
    {
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => RegisterAsync("alice@example.com", ValidPassword))
        );

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1L, await CountUsersAsync());
    }

    [Theory]
    [InlineData("no-at-sign", ValidPassword, "email")]
    [InlineData("a@b@example.com", ValidPassword, "email")]
    [InlineData("alice @example.com", ValidPassword, "email")]
    [InlineData("@example.com", ValidPassword, "email")]
    [InlineData("alice@", ValidPassword, "email")]
    [InlineData(null, ValidPassword, "email")]
    [InlineData("alice@example.com", "fourteen char!", "password")]
    [InlineData("alice@example.com", "", "password")]
    [InlineData("alice@example.com", null, "password")]
    public async Task Register_InvalidField_Returns400WithThatFieldError(
        string? email, string? password, string field)
    {
        var response = await RegisterAsync(email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Equal(new[] { field }, problem!.Errors.Keys);
        Assert.Equal(0L, await CountUsersAsync());
    }

    // Validation, not the CHECK constraint, must reject this:
    // reaching the database would mean a 500
    [Fact]
    public async Task Register_EmailOver254Characters_Returns400()
    {
        var email = new string('a', 249) + "@x.com"; // 255 characters
        
        var response = await RegisterAsync(email, ValidPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> RegisterAsync(string? email, string? password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, password });

    private async Task<(string Email, string PasswordHash)> ReadOnlyUserAsync()
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<(string, string)>("SELECT email, password_hash FROM users");
    }

    private async Task<long> CountUsersAsync()
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<long>("SELECT count(*) FROM users");
    }
}