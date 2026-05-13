using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetBudgetUtilizationRecordById;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.SearchBudgetUtilizationRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.Finance;

public sealed class BudgetUtilizationRecordQueryTests
{
    [Fact]
    public async Task SearchQueryHandler_WithVatKeywordAndFilters_ReturnsExpectedRecord()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var target = BudgetUtilizationRecord.Create(
            burNumber: "BUR-0001",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-ABC-001",
            disbursementVoucherId: null,
            disbursementVoucherNumber: null,
            burDate: new DateOnly(2026, 4, 9),
            allotmentClass: "MOOE",
            uacsObjectCode: "5-02-99-990",
            responsibilityCenter: "FIN-01",
            particulars: "Payment for utilities",
            amount: 1000m,
            remarks: null);

        var other = BudgetUtilizationRecord.Create(
            burNumber: "BUR-0002",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-XYZ-002",
            disbursementVoucherId: null,
            disbursementVoucherNumber: null,
            burDate: new DateOnly(2026, 4, 8),
            allotmentClass: "CO",
            uacsObjectCode: "1-07-05-010",
            responsibilityCenter: "FIN-02",
            particulars: "Capital outlay",
            amount: 2000m,
            remarks: null);

        target.Obligate();

        dbContext.BudgetUtilizationRecords.AddRange(target, other);
        await dbContext.SaveChangesAsync();

        var handler = new SearchBudgetUtilizationRecordsQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(
            new SearchBudgetUtilizationRecordsQuery(
                Keyword: "PO-ABC",
                Status: BudgetUtilizationRecordStatus.Obligated,
                PurchaseOrderId: target.PurchaseOrderId,
                AllotmentClass: "MOOE",
                PageNumber: 1,
                PageSize: 10),
            CancellationToken.None);

        // Assert
        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].BurNumber.ShouldBe("BUR-0001");
        result.Items[0].Status.ShouldBe(BudgetUtilizationRecordStatus.Obligated);
    }

    [Fact]
    public async Task GetByIdQueryHandler_WhenRecordNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetBudgetUtilizationRecordByIdQueryHandler(dbContext);

        // Act
        var action = async () => await handler.Handle(new GetBudgetUtilizationRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByIdQueryHandler_WhenRecordExists_ReturnsMappedDto()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var bur = BudgetUtilizationRecord.Create(
            burNumber: "BUR-GET-01",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-GET-01",
            disbursementVoucherId: null,
            disbursementVoucherNumber: null,
            burDate: new DateOnly(2026, 4, 7),
            allotmentClass: "PS",
            uacsObjectCode: "5-01-01-010",
            responsibilityCenter: "FIN-03",
            particulars: "Payroll obligation",
            amount: 3000m,
            remarks: "for release");

        dbContext.BudgetUtilizationRecords.Add(bur);
        await dbContext.SaveChangesAsync();

        var handler = new GetBudgetUtilizationRecordByIdQueryHandler(dbContext);

        // Act
        var dto = await handler.Handle(new GetBudgetUtilizationRecordByIdQuery(bur.Id), CancellationToken.None);

        // Assert
        dto.Id.ShouldBe(bur.Id);
        dto.BurNumber.ShouldBe("BUR-GET-01");
        dto.PurchaseOrderNumber.ShouldBe("PO-GET-01");
        dto.AllotmentClass.ShouldBe("PS");
        dto.Amount.ShouldBe(3000m);
        dto.Status.ShouldBe(BudgetUtilizationRecordStatus.Draft);
    }

    private static FinanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
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

        return new FinanceDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
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
        public string ApplicationName { get; set; } = nameof(BudgetUtilizationRecordQueryTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

