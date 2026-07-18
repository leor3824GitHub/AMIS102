using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.Multitenancy.Contracts;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// A DI host with real per-scope tenant resolution, for exercising code that switches the ambient tenant —
/// i.e. <c>AssetTransferProjector</c> and its recurring job.
/// <para>
/// The single-tenant helpers elsewhere in this folder construct a <c>DbContext</c> directly with a fixed
/// tenant, which cannot model a scope-switch. Here the accessor and the setter are the SAME scoped object,
/// so writing <c>IMultiTenantContextSetter.MultiTenantContext</c> inside a scope genuinely changes which
/// tenant a <c>DbContext</c> resolved afterwards binds to — exactly the production arrangement.
/// </para>
/// <para>
/// All tenants deliberately share ONE in-memory store and carry an empty connection string (so
/// <c>BaseDbContext.OnConfiguring</c> does not swap providers). Isolation therefore has to come from
/// Finbuckle's <c>.IsMultiTenant()</c> query filter rather than from physically separate databases — which
/// is what makes the leak assertions meaningful instead of vacuous.
/// </para>
/// </summary>
internal sealed class MultiTenantTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public IReadOnlyList<AppTenantInfo> Tenants { get; }
    public ITenantService TenantService { get; }
    public IServiceScopeFactory ScopeFactory => _provider.GetRequiredService<IServiceScopeFactory>();

    /// <param name="configureServices">
    /// Hook for a test to register its own substitutes (mediator, event bus). Runs before the provider is
    /// built — it must be a constructor parameter, not an init property, since init setters run too late.
    /// </param>
    /// <param name="tenantIds">Tenants to put in the registry, all active.</param>
    public MultiTenantTestHost(Action<IServiceCollection>? configureServices, params string[] tenantIds)
    {
        Tenants =
        [
            .. tenantIds.Select(id => new AppTenantInfo(id, id, $"Agency {id.ToUpperInvariant()}")
            {
                IsActive = true,
                ValidUpto = DateTime.UtcNow.AddYears(1),
                ConnectionString = string.Empty   // keeps the InMemory provider in place
            })
        ];

        TenantService = Substitute.For<ITenantService>();
        TenantService.GetAllTenantInfosAsync(Arg.Any<CancellationToken>()).Returns(Tenants);

        var services = new ServiceCollection();

        // One scoped object serving both interfaces — the whole point of this host.
        services.AddScoped<ScopedTenantContext>();
        services.AddScoped<IMultiTenantContextSetter>(sp => sp.GetRequiredService<ScopedTenantContext>());
        services.AddScoped<IMultiTenantContextAccessor<AppTenantInfo>>(sp => sp.GetRequiredService<ScopedTenantContext>());

        var dbName = _databaseName;
        services.AddScoped(sp => new AssetRegisterDbContext(
            sp.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>(),
            new DbContextOptionsBuilder<AssetRegisterDbContext>().UseInMemoryDatabase(dbName).Options,
            Options.Create(new DatabaseOptions { Provider = "InMemory" }),
            new TestHostEnvironment()));

        configureServices?.Invoke(services);
        _provider = services.BuildServiceProvider();
    }

    public AppTenantInfo Tenant(string id) =>
        Tenants.First(t => string.Equals(t.Identifier, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Runs <paramref name="work"/> in a fresh scope bound to <paramref name="tenantId"/>.</summary>
    public async Task AsTenantAsync(string tenantId, Func<AssetRegisterDbContext, Task> work)
    {
        await using var scope = _provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Tenant(tenantId));
        await work(scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>());
    }

    /// <summary>Runs <paramref name="work"/> in a fresh scope bound to <paramref name="tenantId"/> and returns its result.</summary>
    public async Task<T> AsTenantAsync<T>(string tenantId, Func<AssetRegisterDbContext, Task<T>> work)
    {
        await using var scope = _provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Tenant(tenantId));
        return await work(scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>());
    }

    public void Dispose() => _provider.Dispose();

    private sealed class ScopedTenantContext : IMultiTenantContextSetter, IMultiTenantContextAccessor<AppTenantInfo>
    {
        // Never observed: every scope sets the tenant before resolving anything that reads it.
        private IMultiTenantContext _context =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(string.Empty, string.Empty));

        public IMultiTenantContext MultiTenantContext
        {
            get => _context;
            set => _context = value;
        }

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)_context;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(MultiTenantTestHost);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
