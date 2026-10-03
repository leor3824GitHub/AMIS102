using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.IntegrationTests;

/// <summary>
/// Boots the real <see cref="Program"/> pipeline against a throwaway PostgreSQL container.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately does NOT stub the authentication scheme and does NOT replace the authorization
/// handler. The defect this suite exists to catch — see <c>AuthorizationTests</c> — is that the
/// permission handler resolves endpoint metadata to the wrong interface. Any test double over
/// authentication or authorization would hide exactly that, which is how the bug survived since the
/// original port. The only thing substituted here is the database.
/// </para>
/// <para>
/// Environment is <c>Development</c> on purpose: <c>Program.cs</c> applies extra production guards
/// (real signing key, Redis, non-default Hangfire password) that are not what this suite is testing.
/// </para>
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("amis_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // Implemented explicitly: xUnit's IAsyncLifetime returns Task, while the WebApplicationFactory
    // base class already exposes a ValueTask DisposeAsync(). Explicit implementation lets both
    // co-exist instead of one hiding the other.
    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        // Touch Services so the host boots (and the DbInitializers seed the root tenant + admin)
        // once, here, rather than inside the first test that happens to issue a request.
        _ = Services;
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    /// <summary>Tears down the host and the container together.</summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseOptions:Provider"] = "POSTGRESQL",
                ["DatabaseOptions:ConnectionString"] = _postgres.GetConnectionString(),
                // Migrations live in their own assembly; keep the host's own value so EF resolves
                // the same migration set the app uses in production.
                ["DatabaseOptions:MigrationsAssembly"] = "AMIS.Playground.Migrations.PostgreSQL",

                // Empty => AddDistributedMemoryCache. No Redis container needed.
                ["CachingOptions:Redis"] = "",

                // Background jobs and outbound mail are irrelevant here and would only add
                // start-up cost and flakiness.
                ["HangfireOptions:Enabled"] = "false",
                ["OpenTelemetryOptions:Enabled"] = "false",
            });
        });
    }
}

/// <summary>
/// One container and one host for the whole suite — starting Postgres per test class would dominate
/// the run time.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
