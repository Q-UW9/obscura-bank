using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Obscura")
    ?? throw new InvalidOperationException("Missing connection string 'Obscura'.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.MapGet("/api/health/db", async (NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    var name = await conn.ExecuteScalarAsync<string>("SELECT current_database()");
    return Results.Ok(new { status = "ok", database = name });
});

app.Run();
