using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// Guards the auto-queue side-effect of accepting a returned-property receipt: every item the inspector
/// assessed as <see cref="AssetCondition.Unserviceable"/> must land on a Draft IIRUSP/IIRUP so the custodian
/// doesn't have to hunt for it — while items in any other condition stay out of the report, and asset
/// lifecycle stays Available (condemnation only happens on report submit).
/// </summary>
public sealed class AutoQueueUnserviceableOnAcceptTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Accept_QueuesOnlyUnserviceableItems_IntoANewDraftIirup()
    {
        using var db = CreateDbContext();
        var seed = await SeedInspectedReturnAsync(db, "A", AssetCondition.Unserviceable, AssetCondition.NeedingRepair);

        await AcceptAsync(db, seed);

        var report = await db.UnserviceablePropertyReports.Include(r => r.Items).SingleAsync();
        report.ReportType.ShouldBe(UnserviceableReportType.IIRUP);
        report.Status.ShouldBe(UnserviceableReportStatus.Draft);
        report.ReportNo.ShouldBe("IIRUP-AUTO-1");
        // Only the unserviceable asset is queued; the NeedingRepair one is not.
        report.Items.Select(i => i.AssetRegistryId).ShouldBe([seed.UnserviceableAsset.Id]);

        // Both assets flip back to Available at their inspected condition; neither is condemned yet.
        seed.UnserviceableAsset.LifecycleState.ShouldBe(LifecycleState.Available);
        seed.UnserviceableAsset.CurrentCondition.ShouldBe(AssetCondition.Unserviceable);
        seed.OtherAsset.LifecycleState.ShouldBe(LifecycleState.Available);
        seed.OtherAsset.CurrentCondition.ShouldBe(AssetCondition.NeedingRepair);
    }

    [Fact]
    public async Task Accept_WithNoUnserviceableItems_CreatesNoReport()
    {
        using var db = CreateDbContext();
        var seed = await SeedInspectedReturnAsync(db, "B", AssetCondition.InGoodCondition, AssetCondition.NeedingRepair);

        await AcceptAsync(db, seed);

        (await db.UnserviceablePropertyReports.CountAsync()).ShouldBe(0);
    }

    private static async Task AcceptAsync(AssetRegisterDbContext db, SeededReturn seed)
    {
        var freezeGuard = Substitute.For<ICountFreezeGuard>();
        freezeGuard.EnsureMovementAllowedAsync(Arg.Any<IReadOnlyCollection<AssetRegistry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var numbers = Substitute.For<IUnserviceableReportNumberGenerator>();
        var seq = 0;
        numbers.NextAsync(Arg.Any<UnserviceableReportType>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"IIRUP-AUTO-{++seq}");

        var handler = new AcceptReturnedPropertyReceiptCommandHandler(db, freezeGuard, numbers);
        await handler.Handle(
            new AcceptReturnedPropertyReceiptCommand(seed.Receipt.Id, new EmployeeRefDto(seed.CustodianId, "Custodian", "Supply Officer")),
            CancellationToken.None);
    }

    private sealed record SeededReturn(ReturnedPropertyReceipt Receipt, AssetRegistry UnserviceableAsset, AssetRegistry OtherAsset, Guid CustodianId);

    /// <summary>Seeds an accepted PAR (two PPE assets, Assigned) and an Inspected RRP whose two items carry
    /// <paramref name="cond1"/> / <paramref name="cond2"/>. Returns the receipt and the two assets.</summary>
    private static async Task<SeededReturn> SeedInspectedReturnAsync(
        AssetRegisterDbContext db, string suffix, AssetCondition cond1, AssetCondition cond2)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var custodianId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        // Owned EmployeeRef instances must be distinct per navigation (AssignedInspector vs InspectedBy),
        // so build a fresh one at each use rather than sharing a single object.
        var inspectorId = Guid.NewGuid();

        var catalog = PropertyItemCatalog.Create(
            Tenant, $"CAT-{suffix}", "Equipment", "DP", "05", "unit", "1060405", 5);

        var asset1 = AssetRegistry.Register(
            Tenant, catalog, AssetType.PPE, AssetCategory.PPE, PropertyNumber.Create($"2026-06-{suffix}-0001"),
            "Laptop Computer", null, null, null, "01", today, 45000m, null, null);
        var asset2 = AssetRegistry.Register(
            Tenant, catalog, AssetType.PPE, AssetCategory.PPE, PropertyNumber.Create($"2026-06-{suffix}-0002"),
            "Office Table", null, null, null, "01", today, 8000m, null, null);

        // Persist each aggregate in dependency order with its own SaveChanges — mirrors the working
        // seed pattern and keeps the EF in-memory change-tracker from fixing up cross-aggregate graphs.
        db.AssetRegistries.Add(asset1);
        db.AssetRegistries.Add(asset2);
        await db.SaveChangesAsync();

        var accountability = PropertyAccountability.Issue(
            Tenant, AccountabilityType.PPE_PAR, $"PAR-{suffix}", "01",
            EmployeeRef.Create(Guid.NewGuid(), "Issuer", null),
            EmployeeRef.Create(custodianId, "Custodian", "Supply Officer"),
            today, null,
            [(asset1, "1", (string?)null, (VehicleAccountabilityProfile?)null),
             (asset2, "2", (string?)null, (VehicleAccountabilityProfile?)null)]);
        accountability.Accept(today);
        asset1.AssignTo(accountability.Id, custodianId, locationId);
        asset2.AssignTo(accountability.Id, custodianId, locationId);
        db.PropertyAccountabilities.Add(accountability);
        await db.SaveChangesAsync();

        var line1 = accountability.Lines.Single(l => l.AssetRegistryId == asset1.Id);
        var line2 = accountability.Lines.Single(l => l.AssetRegistryId == asset2.Id);

        var receipt = ReturnedPropertyReceipt.Create(
            Tenant, ReturnedPropertyReceiptType.RRP, today, accountability.Id, accountability.DocumentNo,
            EmployeeRef.Create(custodianId, "Custodian", "Supply Officer"),
            EmployeeRef.Create(inspectorId, "Inspector", "Engineer"), remarks: null);
        receipt.AddItem(line1.Id, asset1.Id, 1, asset1.Snapshot());
        receipt.AddItem(line2.Id, asset2.Id, 2, asset2.Snapshot());

        var items = receipt.Items.OrderBy(i => i.ItemNo).ToList();
        receipt.Inspect(EmployeeRef.Create(inspectorId, "Inspector", "Engineer"), new Dictionary<Guid, AssetCondition>
        {
            [items[0].Id] = cond1,
            [items[1].Id] = cond2
        }, remarks: null);

        db.ReturnedPropertyReceipts.Add(receipt);
        await db.SaveChangesAsync();

        // asset1 carries cond1; the test picks whichever asset it flagged Unserviceable.
        var unserviceable = cond1 == AssetCondition.Unserviceable ? asset1 : asset2;
        var other = cond1 == AssetCondition.Unserviceable ? asset2 : asset1;
        return new SeededReturn(receipt, unserviceable, other, custodianId);
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
        public string ApplicationName { get; set; } = nameof(AutoQueueUnserviceableOnAcceptTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
