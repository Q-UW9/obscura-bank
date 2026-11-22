using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Microsoft.Net.Http.Headers;
using Obscura.Api.Data;
using Obscura.Api.Tests.Infrastructure;

namespace Obscura.Api.Tests.Sessions;

[Collection(DatabaseCollection.Name)]
public sealed class SessionHttpTests : IAsyncLifetime
{
    private const string Email = "alice@example.com";
    private const string Password = "correct horse battery staple";
    private const string CookieName = "__Host-session";

    private readonly DatabaseFixture _db;
    private readonly HttpClient _client; // https + cookies sent by hand

    public SessionHttpTests(DatabaseFixture db)
    {
        _db = db;
        _client = db.Factory.CreateHttpsClient();
    }

    public async Task InitializeAsync()
    {
        await _db.ResetDataAsync();
        await _client.PostAsJsonAsync("/api/auth/register", new { email = Email, password = Password });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_SetsTheSessionCookie_WithEveryDecidedFlag()
    {
        var cookie = SessionCookieFrom(await LoginAsync());

        Assert.Equal(43, cookie.Value.Length);
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
        Assert.Equal("/", cookie.Path.ToString());
        Assert.False(cookie.Domain.HasValue);
        Assert.Null(cookie.Expires);
        Assert.Null(cookie.MaxAge);
    }

    [Fact]
    public async Task Me_WithTheSessionCookie_ReturnsTheProfile_WithoutSecrets()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await MeAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(new UserProfile(Email, "user"), JsonSerializer.Deserialize<UserProfile>(body, JsonSerializerOptions.Web));
        Assert.DoesNotContain("argon2id", body);
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(null)).StatusCode);
    }

    // Attack = forged token
    // -> the server only accepts tokens it created itself
    [Theory]
    [InlineData("browser-check")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // 43 chars: well-formed, never issued
    public async Task Me_WithForgedCookie_Returns401(string token)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(token)).StatusCode);
    }

    // Attack = tampering
    // -> changing a single char of a real token makes it worthless
    [Fact]
    public async Task Me_WithOneCharacterOfARealTokenChanged_Returns401()
    {
        var token = await LoginAndGetTokenAsync();
        var tampered = (token[0] == 'A' ? 'B' : 'A') + token[1..];

        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(tampered)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MeAsync(token)).StatusCode);
    }

    // Attack = stolen cookie after logout
    // -> logout ends the session on the server, so a copy is worthless
    [Fact]
    public async Task Logout_ThenReplayingTheOldCookie_Returns401()
    {
        var token = await LoginAndGetTokenAsync();

        var logout = await LogoutAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.True(SessionCookieFrom(logout).Expires < DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(token)).StatusCode);
        Assert.Equal(0L, await CountSessionsAsync());
    }

    [Fact]
    public async Task Session_AfterIdleTimeout_Returns401()
    {
        var token = await LoginAndGetTokenAsync();

        _db.Factory.Clock.Advance(TimeSpan.FromMinutes(15));

        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(token)).StatusCode);
    }

    // Attack = stolen token kept busy
    // -> even a session used every 10 minutes ends 8 hours after login
    [Fact]
    public async Task Session_UsedEvery10Minutes_StillEndsAfter8Hours()
    {
        var token = await LoginAndGetTokenAsync();

        for (var i = 0; i < 47; i++) // 47*10 min = 7 h 50 min
        {
            _db.Factory.Clock.Advance(TimeSpan.FromMinutes(10));
            Assert.Equal(HttpStatusCode.OK, (await MeAsync(token)).StatusCode);
        }

        _db.Factory.Clock.Advance(TimeSpan.FromMinutes(10)); // 8 hours exactly
        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(token)).StatusCode);
    }

    // A cookie-handling client on https://localhost behaves like a browser
    [Fact]
    public async Task BrowserLikeClient_OnHttps_KeepsAndDropsTheSessionCookie()
    {
        var browser = _db.Factory.CreateHttpsClient(handleCookies: true);

        await browser.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/me")).StatusCode);

        await browser.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    // The reason why tests use https:
    // A Secure cookie is never sent over plain http, so this client stays logged out
    [Fact]
    public async Task BrowserLikeClient_OnPlainHttp_NeverSendsTheSecureCookie()
    {
        var browser = _db.Factory.CreateClient(); // http://localhost, with a cookie jar

        var login = await browser.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync(string? carriedToken = null)
    {
        var response = await LoginAsync(carriedToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return SessionCookieFrom(response).Value.ToString();
    }

    private Task<HttpResponseMessage> LoginAsync(string? carriedToken = null) =>
        SendAsync(HttpMethod.Post, "/api/auth/login", carriedToken,
            JsonContent.Create(new { email = Email, password = Password }));

    private Task<HttpResponseMessage> MeAsync(string? token) =>
        SendAsync(HttpMethod.Get, "/api/me", token);

    private Task<HttpResponseMessage> LogoutAsync(string? token) =>
        SendAsync(HttpMethod.Post, "/api/auth/logout", token);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (token is not null)
            request.Headers.Add("Cookie", $"{CookieName}={token}");
        return _client.SendAsync(request);
    }

    private static SetCookieHeaderValue SessionCookieFrom(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var cookie = SetCookieHeaderValue.Parse(header);
        Assert.Equal(CookieName, cookie.Name.ToString());
        return cookie;
    }

    private async Task<long> CountSessionsAsync()
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<long>("SELECT count(*) FROM sessions");
    }
}