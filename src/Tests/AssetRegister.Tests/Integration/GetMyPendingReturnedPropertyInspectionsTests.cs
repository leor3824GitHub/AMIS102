using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Inspections;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using AMIS.Modules.AssetRegister.Features.v1.Inspections;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// Guards the unified inspection worklist's Returned Property producer: it must surface only the <b>calling</b>
/// inspector's Pending return-requests, using the accountability document number as the reference (the receipt
/// number is null until acceptance) and the <c>?inspect=</c> deep-link route.
/// </summary>
public sealed class GetMyPendingReturnedPropertyInspectionsTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_ReturnsOnlyMyPendingRequests_WithAccountabilityRefAndRoute()
    {
        using var db = CreateDbContext();
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();

        await SeedAsync(db, "PAR-MINE", inspectorId: me, reject: false);    // ✅ mine + Pending
        await SeedAsync(db, "PAR-OTHER", inspectorId: other, reject: false); // ✗ Pending but not mine
        await SeedAsync(db, "PAR-DONE", inspectorId: me, reject: true);      // ✗ mine but no longer Pending

        var handler = new GetMyPendingReturnedPropertyInspectionsQueryHandler(db, CurrentUser(), MediatorReturning(me));
        var result = await handler.Handle(new GetMyPendingReturnedPropertyInspectionsQuery(), CancellationToken.None);

        var item = result.ShouldHaveSingleItem();
        item.SourceType.ShouldBe("ReturnedProperty");
        item.Reference.ShouldBe("PAR-MINE");
        item.ActionRoute.ShouldBe($"/asset-register/returned-property?inspect={item.SourceId}");
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenCallerHasNoLinkedEmployee()
    {
        using var db = CreateDbContext();
        await SeedAsync(db, "PAR-1", inspectorId: Guid.NewGuid(), reject: false);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdentityUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((EmployeeReferenceDto?)null);

        var handler = new GetMyPendingReturnedPropertyInspectionsQueryHandler(db, CurrentUser(), mediator);
        var result = await handler.Handle(new GetMyPendingReturnedPropertyInspectionsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static async Task SeedAsync(AssetRegisterDbContext db, string accountabilityDocNo, Guid inspectorId, bool reject)
    {
        var receipt = ReturnedPropertyReceipt.Create(
            Tenant,
            ReturnedPropertyReceiptType.RRP,
            DateOnly.FromDateTime(DateTime.UtcNow),
            accountabilityId: Guid.NewGuid(),
            accountabilityDocumentNo: accountabilityDocNo,
            returnedBy: EmployeeRef.Create(Guid.NewGuid(), "Returner", null),
            assignedInspector: EmployeeRef.Create(inspectorId, "Inspector", null),
            remarks: null);

        if (reject)
            receipt.Reject("Not needed");

        db.ReturnedPropertyReceipts.Add(receipt);
        await db.SaveChangesAsync();
    }

    private static ICurrentUser CurrentUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated().Returns(true);
        user.GetUserId().Returns(Guid.NewGuid());
        return user;
    }

    private static IMediator MediatorReturning(Guid employeeId)
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdentityUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Employee(employeeId));
        return mediator;
    }

    private static EmployeeReferenceDto Employee(Guid id) => new(
        id, "EMP-1", null, "Test", "User", null,
        Guid.NewGuid(), "OFF", "Office", "Office Address", Guid.NewGuid(), "DEP", "Dept",
        Guid.NewGuid(), "POS", "Position", null, null, null, true);

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
        public string ApplicationName { get; set; } = nameof(GetMyPendingReturnedPropertyInspectionsTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
