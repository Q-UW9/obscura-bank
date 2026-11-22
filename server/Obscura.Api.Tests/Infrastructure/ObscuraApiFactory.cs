using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Obscura.Api.Tests.Infrastructure;

// Runs the real API in memory, pointed at obscura_test
public sealed class ObscuraApiFactory : WebApplicationFactory<Program>
{
    // App's clock in tests.
    // It starts at a fixed time and only moves when a test advances it
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so no startup migrations, no developer execution page
        // and no dev connection strings
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Obscura", DatabaseFixture.ConnectionString);

        // Replacing the real clock
        // Program.cs - TimeProvider.System
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    // A client on https://localhost, so Secure cookies are sent
    // handleCookies: false = the test sends cookies by hand (full control for replay attacks)
    //                true  = a browser-like cookie jar
    public HttpClient CreateHttpsClient(bool handleCookies = false) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies,
        });
}