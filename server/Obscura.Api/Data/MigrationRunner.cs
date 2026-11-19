using Dapper;
using Npgsql;

namespace Obscura.Api.Data;

public static class MigrationRunner
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource, string migrationsDir, ILogger logger)
    {
        if (!Directory.Exists(migrationsDir))
            throw new DirectoryNotFoundException($"Migrations folder not found: {migrationsDir}");

        await using var conn = await dataSource.OpenConnectionAsync();

        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version     text        PRIMARY KEY,
                applied_at  timestamptz NOT NULL DEFAULT now()
            )
        """);

        var applied = (await conn.QueryAsync<string>("SELECT version FROM schema_migrations"))
            .ToHashSet();

        var files = Directory.GetFiles(migrationsDir, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var version = Path.GetFileNameWithoutExtension(file);
            if (applied.Contains(version))
                continue;

            var sql = await File.ReadAllTextAsync(file);

            await using var tx = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync(sql, transaction: tx);
            await conn.ExecuteAsync(
                "INSERT INTO schema_migrations (version) VALUES (@version)",
                new {version},
                transaction: tx);
            await tx.CommitAsync();

            logger.LogInformation("Applied migration {Version}", version);
        }
    }
}