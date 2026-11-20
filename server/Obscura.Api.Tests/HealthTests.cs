using System.Net.Http.Json;
using Obscura.Api.Tests.Infrastructure;

namespace Obscura.Api.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class HealthTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    public HealthTests(DatabaseFixture db)
    {
        _db = db;
    }

    public Task InitializeAsync() => _db.ResetDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Guard: the API under test must use obscura_test
    // If this fails, the tests would be touching dev data
    [Fact]
    public async Task Api_UsesTheTestDatabase()
    {
        var client = _db.Factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health/db");

        Assert.Equal("obscura_test", health?.Database);
    }

    // shape of the JSON from GET /api/health/db
    // the API returns an anonymous type, so the test declares its own
    internal sealed record HealthResponse(string Status, string Database);
}