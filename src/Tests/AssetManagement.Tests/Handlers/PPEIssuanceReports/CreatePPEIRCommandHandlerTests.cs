using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Handlers.PPEIssuanceReports;

public sealed class CreatePPEIRCommandHandlerTests
{
    [Fact]
    public async Task Handle_UnissuedPPEItem_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var inventoryItem = CreatePPEItem("PPE-UNISSUED");
        dbContext.TangibleInventoryItems.Add(inventoryItem);
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(inventoryItem.Id);
        var handler = new CreatePPEIRCommandHandler(dbContext, CreateCurrentUser());

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));

        // Assert
        exception.Message.ShouldContain("not yet issued");
    }

    [Fact]
    public async Task Handle_IssuedPPEItemWithoutCustodian_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var inventoryItem = CreatePPEItem("PPE-NO-CUSTODIAN");
        inventoryItem.MarkIssued();

        var registry = AssetRegistry.Create(
            tenantId: TestTenantId,
            tangibleInventoryItemId: inventoryItem.Id,
            itemId: inventoryItem.ItemId,
            propertyNo: inventoryItem.PropertyNo,
            assetType: inventoryItem.AssetType,
            acquisitionDate: inventoryItem.AcquisitionDate,
            unitCost: inventoryItem.UnitCost);

        dbContext.TangibleInventoryItems.Add(inventoryItem);
        dbContext.AssetRegistry.Add(registry);
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(inventoryItem.Id);
        var handler = new CreatePPEIRCommandHandler(dbContext, CreateCurrentUser());

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));

        // Assert
        exception.Message.ShouldContain("no current custodian");
    }

    [Fact]
    public async Task Handle_IssuedPPEItemWithCustodian_CreatesTransferredHistory()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var fromCustodianId = Guid.NewGuid();
        var toCustodianId = Guid.NewGuid();

        var inventoryItem = CreatePPEItem("PPE-TRANSFERRED");
        inventoryItem.MarkIssued();

        var registry = AssetRegistry.Create(
            tenantId: TestTenantId,
            tangibleInventoryItemId: inventoryItem.Id,
            itemId: inventoryItem.ItemId,
            propertyNo: inventoryItem.PropertyNo,
            assetType: inventoryItem.AssetType,
            acquisitionDate: inventoryItem.AcquisitionDate,
            unitCost: inventoryItem.UnitCost);
        registry.AssignTo(fromCustodianId, null);

        dbContext.TangibleInventoryItems.Add(inventoryItem);
        dbContext.AssetRegistry.Add(registry);
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(inventoryItem.Id) with { IssuedToEmployeeId = toCustodianId };
        var handler = new CreatePPEIRCommandHandler(dbContext, CreateCurrentUser());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ItemCount.ShouldBe(1);

        var history = await dbContext.AssetAssignmentHistory.SingleAsync();
        history.EventType.ShouldBe(AssetAssignmentEventType.Transferred);
        history.FromCustodianId.ShouldBe(fromCustodianId);
        history.ToCustodianId.ShouldBe(toCustodianId);
        history.SourceDocumentType.ShouldBe("PPEIR");
    }

    private static TangibleInventoryItem CreatePPEItem(string propertyNo) =>
        TangibleInventoryItem.Create(
            tenantId: TestTenantId,
            tangibleInventoryId: Guid.NewGuid(),
            tangibleItemId: Guid.NewGuid(),
            reference: "PO-001",
            assetType: AssetType.PPE,
            thresholdAmountUsed: 5000m,
            propertyNo: propertyNo,
            itemId: Guid.NewGuid(),
            description: "PPE Item",
            acquisitionDate: new DateOnly(2026, 1, 1),
            quantity: 1,
            unitCost: 15000m);

    private static CreatePPEIRCommand CreateCommand(Guid tangibleInventoryItemId) =>
        new(
            PPEIRNo: $"PPEIR-{Guid.NewGuid():N}"[..18],
            Date: new DateOnly(2026, 5, 9),
            IssuedToEmployeeId: Guid.NewGuid(),
            IssuedToOfficeAddress: "Main Office",
            IssuanceType: PPEIssuanceType.TransferCO,
            IssuedByEmployeeId: Guid.NewGuid(),
            ReceivedByEmployeeId: Guid.NewGuid(),
            ApprovedByEmployeeId: Guid.NewGuid(),
            DateReceived: null,
            DriverName: null,
            BillOfLadingNo: null,
            Items: [new CreatePPEIRItemRequest(tangibleInventoryItemId)]);

    private static ICurrentUser CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetTenant().Returns(TestTenantId);
        currentUser.GetUserId().Returns(Guid.NewGuid());
        return currentUser;
    }

    private static AssetManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
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
            MigrationsAssembly = "AssetManagement.Tests"
        });

        return new AssetManagementDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private const string TestTenantId = "test-tenant";

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
        public string ApplicationName { get; set; } = nameof(CreatePPEIRCommandHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
