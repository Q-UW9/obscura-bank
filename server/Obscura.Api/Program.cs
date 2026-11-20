using Dapper;
using Npgsql;
using Obscura.Api.Auth;
using Obscura.Api.Data;
using Obscura.Api.Security;

// Phase I
// Registering services before Build
var builder = WebApplication.CreateBuilder(args);

// Database connection string from configuration (appsettings.Development.json in dev).
// Fail at startup if it is missing, not on the first request
var connectionString = builder.Configuration.GetConnectionString("Obscura")
    ?? throw new InvalidOperationException("Missing connection string 'Obscura'.");

// One data source for the whole app: it owns the connection pool so it is a singleton
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// Password hashing - both singletons: immutable / stateless, so safe to share between requests
// The container builds PasswordHasher itself and injects Argon2Parameters into its constructor.
builder.Services.AddSingleton(Argon2Parameters.Default);
builder.Services.AddSingleton<PasswordHasher>();

// All SQL for users, stateless
// It opens a pooled connection per call, so a singleton is safe
builder.Services.AddSingleton<UserRepository>();

// Phase II
// Building the app, then middleware and routes
var app = builder.Build();

// Build the hasher (and its dummy hash) now, not during the first login request
_ = app.Services.GetRequiredService<PasswordHasher>();

// Applying migrations at startup, in Development only
// Production migrates as a deliberate deploy step instead
if (app.Environment.IsDevelopment())
{
    var migrationsDir = Path.Combine(AppContext.BaseDirectory, "migrations");
    await MigrationRunner.ApplyAsync(
        app.Services.GetRequiredService<NpgsqlDataSource>(),
        migrationsDir,
        app.Logger);
}

// GET /api/health/db: proves API to PostgreSQL works
// Returns only "ok" and the db name
// Version details should never be returned - security CVE!
app.MapGet("/api/health/db", async (NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    var name = await conn.ExecuteScalarAsync<string>("SELECT current_database()");
    return Results.Ok(new { status = "ok", database = name });
});

// POST /api/auth/register
app.MapAuthEndpoints();

// Start the server; blocks until shutdown
app.Run();
