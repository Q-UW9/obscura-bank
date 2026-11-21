using Dapper;
using Npgsql;
using Obscura.Api.Auth;
using Obscura.Api.Data;
using Obscura.Api.Security;
using Obscura.Api.Sessions;

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

// The app's clock. Tests replace it with a FakeTimeProvider to move time forward.
builder.Services.AddSingleton(TimeProvider.System);

// Session timeouts from appsettings.json, checked at startup
// (fail fast on a missing or wrong section)
builder.Services.AddOptions<SessionTimeoutOptions>()
    .Bind(builder.Configuration.GetSection(SessionTimeoutOptions.SectionName))
    .Validate(o => o.IdleTimeout > TimeSpan.Zero && o.AbsoluteTimeout > o.IdleTimeout,
        "Sessions: IdleTimeout must be positive and shorter than AbsoluteTimeout.")
    .ValidateOnStart();

// All SQL for sessions. Stateless (pooled connection per call)
// A singleton like the others
builder.Services.AddSingleton<SessionStore>();

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

// ARCHIVED
// Two dev-only endpoints that verified, before any session code existed, that
// browsers accept the session cookie as decided: "__Host-session" with 
// Secure, HttpOnly, SameSite=Strict, Path=/ and no Expires, over plain
// http://localhost through the Vite proxy.
// Result = accepted and sent back in Brave (Chromium) and Firefox, hidden from
// document.cookie.
// Kept as a record of that check, though the following code is safe to delete.
// The real cookie is set by the login endpoint.
// if (app.Environment.IsDevelopment())
// {
//     app.MapGet("/api/dev/cookie-check/set", (HttpContext http) =>
//     {
//         http.Response.Cookies.Append("__Host-session", "browser-check", new CookieOptions
//         {
//             Secure = true,
//             HttpOnly = true,
//             SameSite = SameSiteMode.Strict,
//             Path = "/",
//         });
//         return Results.Text("Cookie set. Now open /api/dev/cookie-check/echo");
//     });

//     app.MapGet("/api/dev/cookie-check/echo", (HttpContext http) =>
//         Results.Ok(new { received = http.Request.Cookies.ContainsKey("__Host-session") }));
// }

// Start the server; blocks until shutdown
app.Run();
