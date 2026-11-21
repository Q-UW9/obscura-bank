using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Obscura.Api.Sessions;

// The user behind a valid session.
// The authentication handler turns this into the ClaimsPrincipal.
public sealed record SessionUser(long UserId, string Role);

// All SQL for the sessions table, plus the expiry rules (E2)
public sealed class SessionStore
{
    // last_seen_at is written at most this often, not on every request.
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    private readonly NpgsqlDataSource _db;
    private readonly TimeProvider _time;
    private readonly SessionTimeoutOptions _options;

    public SessionStore(NpgsqlDataSource db, TimeProvider time, IOptions<SessionTimeoutOptions> options)
    {
        _db = db;
        _time = time;
        _options = options.Value;
    }

    // Starts a new session and returns the token for the cookie.
    // Only its hash is stored.
    public async Task<string> CreateAsync(long userId)
    {
        var token = SessionToken.Create();
        // a token from Create() is always well-formed
        var tokenHash = SessionToken.TryHash(token)!;
        var now = _time.GetUtcNow().UtcDateTime;

        await using var conn = await _db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            """
            INSERT INTO sessions (user_id, token_hash, created_at, last_seen_at, expires_at)
            VALUES (@userId, @tokenHash, @now, @now, @expiresAt)
            """,
            new { userId, tokenHash, now, expiresAt = now + _options.AbsoluteTimeout });

        return token;
    }

    // The user behind a token hash or null if there was no such session or it has
    // expired.
    // Expired sessions are deleted on the spot.
    public async Task<SessionUser?> FindValidAsync(byte[] tokenHash)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        await using var conn = await _db.OpenConnectionAsync();
        var row = await conn.QuerySingleOrDefaultAsync<SessionRow>(
            """
            SELECT s.id, s.user_id AS UserId, u.role, s.last_seen_at AS LastSeenAt,
                s.expires_at AS ExpiresAt
            FROM sessions s
            JOIN users u ON u.id = s.user_id
            WHERE s.token_hash = @tokenHash
            """,
            new { tokenHash });
        
        if (row is null)
            return null;

        var idleExpired = now >= row.LastSeenAt + _options.IdleTimeout;
        var absoluteExpired = now >= row.ExpiresAt;
        if (idleExpired || absoluteExpired)
        {
            await conn.ExecuteAsync("DELETE FROM sessions WHERE id = @id", new { id = row.Id });
            return null;
        }

        if (now - row.LastSeenAt >= TouchInterval)
            await conn.ExecuteAsync(
                "UPDATE sessions SET last_seen_at = @now WHERE id = @id",
                new { now, id = row.Id });
            
        return new SessionUser(row.UserId, row.Role);
    }

    // Ends a sessions: logout and the rotation at login against fixation.
    // Unknown hashes are ignored.
    public async Task DeleteAsync(byte[] tokenHash)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM sessions WHERE token_hash = @tokenHash", new { tokenHash });
    }

    private sealed record SessionRow(long Id, long UserId, string Role, DateTime LastSeenAt, DateTime ExpiresAt);
}