using Dapper;
using Npgsql;

namespace Obscura.Api.Data;

// This is what is shown by GET /api/me
// Never the password hash!
public sealed record UserProfile(string Email, string Role);

// What a login needs from a user row. More columns join when a feature needs them
public sealed record UserCredentials(long Id, string Email, string PasswordHash);

// All SQL for the users table - every value goes in as a parameter, never into the SQL text
public sealed class UserRepository
{
    private const string EmailUniqueConstraint = "users_email_unique";

    private readonly NpgsqlDataSource _db;

    public UserRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // Insert a user and return the new id OR null when the email is already taken
    // The unique constraint decides, not a SELECT beforehand
    public async Task<long?> CreateAsync(string email, string passwordHash)
    {
        await using var conn = await _db.OpenConnectionAsync();
        try
        {
            return await conn.ExecuteScalarAsync<long>(
                "INSERT INTO users (email, password_hash) VALUES (@email, @passwordHash) RETURNING id",
                new { email, passwordHash });
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation
            && e.ConstraintName == EmailUniqueConstraint)
        {
            return null;
        }
    }

    // Expect an already normalized email (trim + lowercase)
    // The CHECK constraint enforces that on insert
    public async Task<UserCredentials?> FindByEmailAsync(string email)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<UserCredentials>(
            """
            SELECT id, email, password_hash AS PasswordHash
            FROM users
            WHERE email = @email
            """,
            new { email });
    }

    // The profile of a logged-in user or null if the user no longer exists
    public async Task<UserProfile?> FindProfileAsync(long id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<UserProfile>(
            "SELECT email, role FROM users WHERE id = @id",
            new { id });
    }

    // Used after a successful login when the stored hash has oudated parameters
    public async Task UpdatePasswordHashAsync(long id, string passwordHash)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            "UPDATE users SET password_hash = @passwordHash WHERE id = @id",
            new { id, passwordHash });
    }
}