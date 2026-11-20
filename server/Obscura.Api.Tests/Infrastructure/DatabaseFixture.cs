using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Obscura.Api.Data;

namespace Obscura.Api.Tests.Infrastructure;

// Shared by every database test: one instance per test run
public sealed class DatabaseFixture : IAsyncLifetime
{
    // Dev-only credentials, as in docker-compose.yml
    // Always obscura_test: the tests wipe it
    public const string ConnectionString = 
        "Host=localhost;Port=5432;Database=obscura_test;Username=obscura;Password=obscura_dev";
    
    public NpgsqlDataSource DataSource { get; } = NpgsqlDataSource.Create(ConnectionString);
    public ObscuraApiFactory Factory { get; } = new();

    // Once per run: an empty schema, then every migration, as if it were a new db
    public async Task InitializeAsync()
    {
        await using (var conn = await DataSource.OpenConnectionAsync())
        {
            await conn.ExecuteAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        }

        var migrationsDir = Path.Combine(AppContext.BaseDirectory, "migrations");
        await MigrationRunner.ApplyAsync(DataSource, migrationsDir, NullLogger.Instance);
    }

    // Before each test, delete every row in every data table
    // Only the schema and the schema_migrations stay
    public async Task ResetDataAsync()
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync("""
            DO $$
            DECLARE tables text;
            BEGIN
                SELECT string_agg(format('%I', tablename), ', ') INTO tables
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename <> 'schema_migrations';

                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await DataSource.DisposeAsync();
    }
}