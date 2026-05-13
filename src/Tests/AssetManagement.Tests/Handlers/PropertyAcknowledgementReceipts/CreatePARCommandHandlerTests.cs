using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Handlers.PropertyAcknowledgementReceipts;

public sealed class CreatePARCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoPreviousCustodian_CreatesAssignedHistory()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var catalog = PropertyItemCatalog.Create(TestTenantId, "PPE-001", "Laptop", null, null, "Unit", 5);
        var receivedByEmployeeId = Guid.NewGuid();
        var inventoryItem = CreatePPEItem(catalog.Id, "PPE-ASSIGNED");

        dbContext.PropertyItemCatalog.Add(catalog);
        dbContext.TangibleInventoryItems.Add(inventoryItem);
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(inventoryItem.Id, receivedByEmployeeId);
        var handler = new CreatePARCommandHandler(dbContext, CreateCurrentUser());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ItemCount.ShouldBe(1);

        var history = await dbContext.AssetAssignmentHistory.SingleAsync();
        history.EventType.ShouldBe(AssetAssignmentEventType.Assigned);
        history.FromCustodianId.ShouldBeNull();
        history.ToCustodianId.ShouldBe(receivedByEmployeeId);
    }

    [Fact]
    public async Task Handle_WithPreviousCustodian_CreatesTransferredHistory()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var catalog = PropertyItemCatalog.Create(TestTenantId, "PPE-002", "Desktop", null, null, "Unit", 5);
        var previousCustodianId = Guid.NewGuid();
        var receivedByEmployeeId = Guid.NewGuid();
        var inventoryItem = CreatePPEItem(catalog.Id, "PPE-TRANSFERRED");

        var registry = AssetRegistry.Create(
            tenantId: TestTenantId,
            tangibleInventoryItemId: inventoryItem.Id,
            itemId: inventoryItem.ItemId,
            propertyNo: inventoryItem.PropertyNo,
            assetType: inventoryItem.AssetType,
            acquisitionDate: inventoryItem.AcquisitionDate,
            unitCost: inventoryItem.UnitCost);
        registry.AssignTo(previousCustodianId, null);

        dbContext.PropertyItemCatalog.Add(catalog);
        dbContext.TangibleInventoryItems.Add(inventoryItem);
        dbContext.AssetRegistry.Add(registry);
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(inventoryItem.Id, receivedByEmployeeId);
        var handler = new CreatePARCommandHandler(dbContext, CreateCurrentUser());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ItemCount.ShouldBe(1);

        var history = await dbContext.AssetAssignmentHistory.SingleAsync();
        history.EventType.ShouldBe(AssetAssignmentEventType.Transferred);
        history.FromCustodianId.ShouldBe(previousCustodianId);
        history.ToCustodianId.ShouldBe(receivedByEmployeeId);
    }

    private static TangibleInventoryItem CreatePPEItem(Guid catalogItemId, string propertyNo) =>
        TangibleInventoryItem.Create(
            tenantId: TestTenantId,
            tangibleInventoryId: Guid.NewGuid(),
            tangibleItemId: Guid.NewGuid(),
            reference: "PO-001",
            assetType: AssetType.PPE,
            thresholdAmountUsed: 5000m,
            propertyNo: propertyNo,
            itemId: catalogItemId,
            description: "PPE Item",
            acquisitionDate: new DateOnly(2026, 1, 1),
            quantity: 1,
            unitCost: 15000m);

    private static CreatePARCommand CreateCommand(Guid tangibleInventoryItemId, Guid receivedByEmployeeId) =>
        new(
            PARNo: $"PAR-{Guid.NewGuid():N}"[..12],
            Date: new DateOnly(2026, 5, 9),
            PARType: PARType.Transfer,
            ReceivedFromEmployeeId: Guid.NewGuid(),
            ReceivedByEmployeeId: receivedByEmployeeId,
            ApprovedByEmployeeId: Guid.NewGuid(),
            Items:
            [
                new CreatePARItemRequest(tangibleInventoryItemId, 1, "Unit", "PPE Item")
            ]);

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
        public string ApplicationName { get; set; } = nameof(CreatePARCommandHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
