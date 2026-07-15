using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Features.v1.Accountability.GetExpiringAccountabilities;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// Guards the renewal-reminder producer: it must surface only <b>Active</b> ICS/PAR whose expiry is on or
/// before <c>today + WithinDays</c> (including already-overdue ones), soonest-expiry first, and exclude
/// far-future and not-yet-accepted (PendingAcceptance) documents.
/// </summary>
public sealed class GetExpiringAccountabilitiesTests
{
    private const string Tenant = "root";

    [Fact]
    public async Task Handle_ReturnsActiveDueOrOverdue_SortedSoonestFirst_ExcludingFarFutureAndPending()
    {
        using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedActiveIcsAsync(db, "ICS-OVERDUE", "001", expiresOn: today.AddDays(-5));  // ✅ overdue
        await SeedActiveIcsAsync(db, "ICS-SOON", "002", expiresOn: today.AddDays(30));      // ✅ within window
        await SeedActiveIcsAsync(db, "ICS-FAR", "003", expiresOn: today.AddDays(120));      // ✗ beyond window
        await SeedPendingIcsAsync(db, "ICS-PENDING", "004", expiresOn: today.AddDays(10));  // ✗ not yet Active

        var handler = new GetExpiringAccountabilitiesQueryHandler(db);
        var result = await handler.Handle(new GetExpiringAccountabilitiesQuery(WithinDays: 60), CancellationToken.None);

        result.Select(a => a.DocumentNo).ShouldBe(["ICS-OVERDUE", "ICS-SOON"]); // soonest-expiry first
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNothingDueWithinWindow()
    {
        using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedActiveIcsAsync(db, "ICS-FAR", "001", expiresOn: today.AddDays(200));

        var handler = new GetExpiringAccountabilitiesQueryHandler(db);
        var result = await handler.Handle(new GetExpiringAccountabilitiesQuery(WithinDays: 60), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static async Task SeedActiveIcsAsync(AssetRegisterDbContext db, string docNo, string suffix, DateOnly expiresOn)
    {
        var acc = NewIcs(docNo, suffix, expiresOn);
        acc.Accept(DateOnly.FromDateTime(DateTime.UtcNow));
        db.PropertyAccountabilities.Add(acc);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPendingIcsAsync(AssetRegisterDbContext db, string docNo, string suffix, DateOnly expiresOn)
    {
        db.PropertyAccountabilities.Add(NewIcs(docNo, suffix, expiresOn)); // left in PendingAcceptance
        await db.SaveChangesAsync();
    }

    private static PropertyAccountability NewIcs(string docNo, string suffix, DateOnly expiresOn) =>
        PropertyAccountability.Issue(
            tenantId: Tenant,
            type: AccountabilityType.SE_ICS,
            documentNo: docNo,
            fundCluster: "01",
            issuedBy: EmployeeRef.Create(Guid.NewGuid(), "Supply Officer", "Officer"),
            receivedBy: EmployeeRef.Create(Guid.NewGuid(), "End User", "Clerk"),
            issuedOn: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            expiresOn: expiresOn,
            lines: [(NewSeAsset(suffix), "1", null, null)]);

    private static AssetRegistry NewSeAsset(string suffix)
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: Tenant,
            code: $"CHAIR-{suffix}",
            description: "Monobloc Chair",
            defaultPropertyClass: "07-SE",
            defaultCategoryCode: "CHR",
            defaultUnit: "pc",
            uacsObjectCode: "10405030",
            estimatedUsefulLifeYears: 5);

        var propertyNo = PropertyNumber.Create($"2026-NFA-00B-07-CHR-{suffix}");

        return AssetRegistry.Register(
            tenantId: Tenant,
            catalog: catalog,
            assetType: AssetType.SE,
            category: AssetCategory.LowValuedSemi,
            propertyNo: propertyNo,
            description: "Monobloc Chair",
            serialNo: null,
            brand: null,
            model: null,
            fundCluster: "01",
            acquisitionDate: new DateOnly(2026, 1, 15),
            unitCost: 1500m,
            sourceIARId: null,
            sourcePurchaseOrderId: null);
    }

    private static AssetRegisterDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetRegisterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenant = new AppTenantInfo(Tenant, Tenant, "Test Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var databaseOptions = Options.Create(new DatabaseOptions { Provider = "InMemory" });

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
        public string ApplicationName { get; set; } = nameof(GetExpiringAccountabilitiesTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
