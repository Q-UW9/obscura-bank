using System.Buffers.Text;
using Dapper;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Obscura.Api.Sessions;
using Obscura.Api.Tests.Infrastructure;

namespace Obscura.Api.Tests.Sessions;

[Collection(DatabaseCollection.Name)]
public sealed class SessionStoreTests : IAsyncLifetime
{
    private static readonly TimeSpan Idle = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Absolute = TimeSpan.FromHours(8);

    private readonly DatabaseFixture _db;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));
    private readonly SessionStore _store;
    private long _userId;

    public SessionStoreTests(DatabaseFixture db)
    {
        _db = db;
        var options = Options.Create(new SessionTimeoutOptions { IdleTimeout = Idle, AbsoluteTimeout = Absolute });
        _store = new SessionStore(db.DataSource, _clock, options);
    }

    public async Task InitializeAsync()
    {
        await _db.ResetDataAsync();
        _userId = await InsertUserAsync("alice@example.com");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_ThenFind_ReturnsTheUserAndRole()
    {
        var token = await _store.CreateAsync(_userId);

        var user = await _store.FindValidAsync(Hash(token));

        Assert.Equal(new SessionUser(_userId, "user"), user);
    }

    // Attack = stolen database:
    // the table holds SHA-256(token), never the token itself
    [Fact]
    public async Task Create_StoresOnlyTheHashOfTheToken()
    {
        var token = await _store.CreateAsync(_userId);

        var stored = await QuerySingleAsync<byte[]>("SELECT token_hash FROM sessions");

        Assert.Equal(Hash(token), stored);
        Assert.NotEqual(Base64Url.DecodeFromChars(token), stored);
    }

    [Fact]
    public async Task Find_UnknownToken_ReturnsNull()
    {
        await _store.CreateAsync(_userId);

        Assert.Null(await _store.FindValidAsync(Hash(SessionToken.Create())));
    }

    [Fact]
    public async Task Find_AfterIdleTimeout_ReturnsNull_AndDeletesTheRow()
    {
        var token = await _store.CreateAsync(_userId);

        _clock.Advance(Idle);

        Assert.Null(await _store.FindValidAsync(Hash(token)));
        Assert.Equal(0L, await QuerySingleAsync<long>("SELECT count(*) FROM sessions"));
    }

    [Fact]
    public async Task Find_OneSecondBeforeIdleTimeout_IsStillValid()
    {
        var token = await _store.CreateAsync(_userId);

        _clock.Advance(Idle - TimeSpan.FromSeconds(1));

        Assert.NotNull(await _store.FindValidAsync(Hash(token)));
    }

    // Sliding expiry:
    // each request pushes the idle timeout forward
    [Fact]
    public async Task Find_RegularActivity_KeepsTheSessionAlive()
    {
        var token = await _store.CreateAsync(_userId);

        _clock.Advance(TimeSpan.FromMinutes(10));
        Assert.NotNull(await _store.FindValidAsync(Hash(token)));

        // 20 min after login, 10 min after last request
        _clock.Advance(TimeSpan.FromMinutes(10));
        Assert.NotNull(await _store.FindValidAsync(Hash(token)));
    }

    // Attack = stolen token kept busy:
    // Even a session used every 10 minutes ends 8 hours after login
    [Fact]
    public async Task Find_AfterAbsoluteTimeout_ReturnsNull_EvenWhenActive()
    {
        var token = await _store.CreateAsync(_userId);
        var step = TimeSpan.FromMinutes(10);
        var elapsed = TimeSpan.Zero;

        while (elapsed + step < Absolute)
        {
            _clock.Advance(step);
            elapsed += step;
            Assert.NotNull(await _store.FindValidAsync(Hash(token)));
        }

        _clock.Advance(Absolute - elapsed); // exactly 8 h after login
        Assert.Null(await _store.FindValidAsync(Hash(token)));
    }

    [Fact]
    public async Task Find_WritesLastSeenAtMostOncePerMinute()
    {
        var token = await _store.CreateAsync(_userId);
        var loginTime = _clock.GetUtcNow().UtcDateTime;

        _clock.Advance(TimeSpan.FromSeconds(30));
        await _store.FindValidAsync(Hash(token));
        Assert.Equal(loginTime, await ReadLastSeenAsync());

        _clock.Advance(TimeSpan.FromSeconds(31)); // 61 s total since last write
        await _store.FindValidAsync(Hash(token));
        Assert.Equal(loginTime.AddSeconds(61), await ReadLastSeenAsync());
    }

    [Fact]
    public async Task Delete_EndsTheSession()
    {
        var token = await _store.CreateAsync(_userId);

        await _store.DeleteAsync(Hash(token));

        Assert.Null(await _store.FindValidAsync(Hash(token)));
    }

    // The role is read from users on every request, so a change takes effect
    // immediately.
    [Fact]
    public async Task Find_ReadsTheCurrentRole()
    {
        var token = await _store.CreateAsync(_userId);

        await using (var conn = await _db.DataSource.OpenConnectionAsync())
        {
            await conn.ExecuteAsync("UPDATE users SET role = 'admin' WHERE id = @id", new { id = _userId });
        }

        Assert.Equal("admin", (await _store.FindValidAsync(Hash(token)))!.Role);
    }

    private static byte[] Hash(string token) => SessionToken.TryHash(token)!;

    private async Task<long> InsertUserAsync(string email)
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<long>(
            "INSERT INTO users (email, password_hash) VALUES (@email, 'not-a-real-hash') RETURNING id",
            new { email });
    }

    private async Task<T> QuerySingleAsync<T>(string sql)
    {
        await using var conn = await _db.DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<T>(sql);
    }

    private Task<DateTime> ReadLastSeenAsync() =>
        QuerySingleAsync<DateTime>("SELECT last_seen_at FROM sessions");
}