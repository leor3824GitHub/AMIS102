using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Data;

/// <summary>
/// DB-level tests for <see cref="DepreciationPostingService"/> against the real
/// <see cref="AssetRegisterDbContext"/> (EF Core in-memory provider): catch-up posting,
/// idempotency, residual-floor termination, and SE exclusion.
/// </summary>
public sealed class DepreciationPostingServiceTests
{
    [Fact]
    public async Task PostThrough_CatchesUpMonthByMonth_AndIsIdempotent()
    {
        await using var db = CreateDbContext();
        // 60,000 @ 5yr, 5% residual (3,000) → straight-line 950/month. Acquired Jan 2025 → starts Feb 2025.
        db.AssetRegistries.Add(NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2025, 1, 15)));
        await db.SaveChangesAsync();
        var service = new DepreciationPostingService(db);

        // Catch up Feb–Dec 2025 = 11 months.
        var first = await service.PostThroughAsync(new DateOnly(2025, 12, 1), CancellationToken.None);

        first.EntriesPosted.ShouldBe(11);
        first.AssetsProcessed.ShouldBe(1);
        var asset = await db.AssetRegistries.SingleAsync();
        asset.AccumulatedDepreciation.ShouldBe(10_450m); // 11 × 950
        asset.CarryingAmount.ShouldBe(49_550m);
        asset.DepreciatedThrough.ShouldBe(new DateOnly(2025, 12, 1));
        (await db.DepreciationEntries.CountAsync()).ShouldBe(11);

        // Re-running for the same period posts nothing (cursor starts after DepreciatedThrough).
        var rerun = await service.PostThroughAsync(new DateOnly(2025, 12, 1), CancellationToken.None);
        rerun.EntriesPosted.ShouldBe(0);
        (await db.DepreciationEntries.CountAsync()).ShouldBe(11);

        // Advancing one month posts exactly one more.
        var next = await service.PostThroughAsync(new DateOnly(2026, 1, 1), CancellationToken.None);
        next.EntriesPosted.ShouldBe(1);
        (await db.DepreciationEntries.CountAsync()).ShouldBe(12);
    }

    [Fact]
    public async Task PostThrough_StopsAtResidual_OverFullLife()
    {
        await using var db = CreateDbContext();
        // 12,000 @ 1yr, 5% residual (600) → 950/month, depreciable 11,400 over 12 months.
        db.AssetRegistries.Add(NewPpe(unitCost: 12_000m, lifeYears: 1, acquisition: new DateOnly(2025, 1, 15)));
        await db.SaveChangesAsync();
        var service = new DepreciationPostingService(db);

        // Post well past the useful life — must self-terminate at the residual floor.
        var result = await service.PostThroughAsync(new DateOnly(2027, 1, 1), CancellationToken.None);

        result.EntriesPosted.ShouldBe(12);
        var asset = await db.AssetRegistries.SingleAsync();
        asset.AccumulatedDepreciation.ShouldBe(11_400m);
        asset.CarryingAmount.ShouldBe(600m); // == residual
        asset.IsFullyDepreciated.ShouldBeTrue();
    }

    [Fact]
    public async Task PostThrough_SkipsSemiExpendable()
    {
        await using var db = CreateDbContext();
        db.AssetRegistries.Add(NewSe(unitCost: 4_000m, acquisition: new DateOnly(2025, 1, 15)));
        await db.SaveChangesAsync();
        var service = new DepreciationPostingService(db);

        var result = await service.PostThroughAsync(new DateOnly(2025, 12, 1), CancellationToken.None);

        result.EntriesPosted.ShouldBe(0);
        (await db.DepreciationEntries.CountAsync()).ShouldBe(0);
        (await db.AssetRegistries.SingleAsync()).AccumulatedDepreciation.ShouldBe(0m);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static AssetRegistry NewPpe(decimal unitCost, int lifeYears, DateOnly acquisition)
    {
        var catalog = PropertyItemCatalog.Create(
            TestTenantId, "DESK-001", "Office Desk", "07-PPE", "DSK", "pc", "10405030", lifeYears);
        return AssetRegistry.Register(
            TestTenantId, catalog, AssetType.PPE, AssetCategory.PPE,
            PropertyNumber.Create("2025-NFA-00B-07-DSK-001"), "Office Desk",
            null, null, null, "01", acquisition, unitCost, null, null);
    }

    private static AssetRegistry NewSe(decimal unitCost, DateOnly acquisition)
    {
        var catalog = PropertyItemCatalog.Create(
            TestTenantId, "CHAIR-001", "Monobloc Chair", "SE", "CHR", "pc", "10405020", 5);
        return AssetRegistry.Register(
            TestTenantId, catalog, AssetType.SE, AssetCategory.LowValuedSemi,
            PropertyNumber.Create("2025-NFA-00B-SE-CHR-001"), "Monobloc Chair",
            null, null, null, "01", acquisition, unitCost, null, null);
    }

    private const string TestTenantId = "test-tenant";

    private static AssetRegisterDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetRegisterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenant = new AppTenantInfo(TestTenantId, TestTenantId, "Test Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var databaseOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "AssetRegister.Tests"
        });

        return new AssetRegisterDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private sealed class TestTenantContextAccessor(IMultiTenantContext<AppTenantInfo> multiTenantContext)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = multiTenantContext;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(DepreciationPostingServiceTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
