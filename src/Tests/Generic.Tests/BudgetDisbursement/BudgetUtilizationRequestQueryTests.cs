using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetBudgetUtilizationRequestById;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.SearchBudgetUtilizationRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.BudgetDisbursement;

public sealed class BudgetUtilizationRequestQueryTests
{
    [Fact]
    public async Task SearchQueryHandler_WithVatKeywordAndFilters_ReturnsExpectedRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var target = BudgetUtilizationRequest.Create(
            tenantId: "test-tenant",
            burNumber: "BUR-0001",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-ABC-001",
            burDate: new DateOnly(2026, 4, 9),
            allotmentClass: "MOOE",
            uacsObjectCode: "5-02-99-990",
            responsibilityCenter: "FIN-01",
            particulars: "Payment for utilities",
            amount: 1000m,
            remarks: null);

        var other = BudgetUtilizationRequest.Create(
            tenantId: "test-tenant",
            burNumber: "BUR-0002",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-XYZ-002",
            burDate: new DateOnly(2026, 4, 8),
            allotmentClass: "CO",
            uacsObjectCode: "1-07-05-010",
            responsibilityCenter: "FIN-02",
            particulars: "Capital outlay",
            amount: 2000m,
            remarks: null);

        target.Obligate();

        dbContext.BudgetUtilizationRequests.AddRange(target, other);
        await dbContext.SaveChangesAsync();

        var handler = new SearchBudgetUtilizationRequestsQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(
            new SearchBudgetUtilizationRequestsQuery(
                Keyword: "PO-ABC",
                Status: BudgetUtilizationRequestStatus.Obligated,
                PurchaseOrderId: target.PurchaseOrderId,
                AllotmentClass: "MOOE",
                PageNumber: 1,
                PageSize: 10),
            CancellationToken.None);

        // Assert
        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].BurNumber.ShouldBe("BUR-0001");
        result.Items[0].Status.ShouldBe(BudgetUtilizationRequestStatus.Obligated);
    }

    [Fact]
    public async Task GetByIdQueryHandler_WhenRecordNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetBudgetUtilizationRequestByIdQueryHandler(dbContext);

        // Act
        var action = async () => await handler.Handle(new GetBudgetUtilizationRequestByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert — the handler surfaces a framework NotFoundException (HTTP 404), not a bare BCL exception.
        await action.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task GetByIdQueryHandler_WhenRecordExists_ReturnsMappedDto()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var bur = BudgetUtilizationRequest.Create(
            tenantId: "test-tenant",
            burNumber: "BUR-GET-01",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-GET-01",
            burDate: new DateOnly(2026, 4, 7),
            allotmentClass: "PS",
            uacsObjectCode: "5-01-01-010",
            responsibilityCenter: "FIN-03",
            particulars: "Payroll obligation",
            amount: 3000m,
            remarks: "for release");

        dbContext.BudgetUtilizationRequests.Add(bur);
        await dbContext.SaveChangesAsync();

        var handler = new GetBudgetUtilizationRequestByIdQueryHandler(dbContext);

        // Act
        var dto = await handler.Handle(new GetBudgetUtilizationRequestByIdQuery(bur.Id), CancellationToken.None);

        // Assert
        dto.Id.ShouldBe(bur.Id);
        dto.BurNumber.ShouldBe("BUR-GET-01");
        dto.PurchaseOrderNumber.ShouldBe("PO-GET-01");
        dto.AllotmentClass.ShouldBe("PS");
        dto.Amount.ShouldBe(3000m);
        dto.Status.ShouldBe(BudgetUtilizationRequestStatus.Draft);
    }

    private static BudgetDisbursementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDisbursementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenant = new AppTenantInfo("test-tenant", "test-tenant", "Test Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };

        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));

        var databaseOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "Generic.Tests"
        });

        return new BudgetDisbursementDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
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
        public string ApplicationName { get; set; } = nameof(BudgetUtilizationRequestQueryTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

