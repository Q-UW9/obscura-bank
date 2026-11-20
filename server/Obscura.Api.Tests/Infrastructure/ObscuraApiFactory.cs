using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Obscura.Api.Tests.Infrastructure;

// Runs the real API in memory, pointed at obscura_test
public sealed class ObscuraApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so no startup migrations, no developer execution page
        // and no dev connection strings
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Obscura", DatabaseFixture.ConnectionString);
    }
}