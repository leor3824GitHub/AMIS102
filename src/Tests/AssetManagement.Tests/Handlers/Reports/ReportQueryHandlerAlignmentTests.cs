using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;
using AMIS.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;
using AMIS.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Handlers.Reports;

public sealed class ReportQueryHandlerAlignmentTests
{
    [Fact]
    public async Task Handle_RSPIQuery_OrdersByDateAndIcsNo_AndBuildsSectionsAndSignatories()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var issuedFromEmployeeId = Guid.NewGuid();
        var receivedByEmployeeId = Guid.NewGuid();

        var catalog = PropertyItemCatalog.Create(TestTenantId, "LAP-001", "Laptop", null, null, "Unit", 5);
        var inventoryItemA = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-1",
            AssetType.SE,
            5000m,
            "PROP-002",
            catalog.Id,
            "Laptop A",
            new DateOnly(2026, 1, 2),
            1,
            2000m);
        var inventoryItemB = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-2",
            AssetType.SE,
            5000m,
            "PROP-001",
            catalog.Id,
            "Laptop B",
            new DateOnly(2026, 1, 2),
            1,
            3000m);

        var icsB = InventoryCustodianSlip.Create(
            TestTenantId,
            "ICS-002",
            new DateOnly(2026, 1, 10),
            AssetCategory.LowValuedSemi,
            "FC-1",
            issuedFromEmployeeId,
            receivedByEmployeeId);
        var icsA = InventoryCustodianSlip.Create(
            TestTenantId,
            "ICS-001",
            new DateOnly(2026, 1, 10),
            AssetCategory.LowValuedSemi,
            "FC-1",
            issuedFromEmployeeId,
            receivedByEmployeeId);

        var icsItemB = ICSItem.Create(
            TestTenantId,
            icsB.Id,
            inventoryItemA.Id,
            1,
            "Issued B",
            2000m,
            3,
            AssetType.SE);
        var icsItemA = ICSItem.Create(
            TestTenantId,
            icsA.Id,
            inventoryItemB.Id,
            1,
            "Issued A",
            3000m,
            3,
            AssetType.SE);

        dbContext.PropertyItemCatalog.Add(catalog);
        dbContext.TangibleInventoryItems.AddRange(inventoryItemA, inventoryItemB);
        dbContext.InventoryCustodianSlips.AddRange(icsB, icsA);
        dbContext.ICSItems.AddRange(icsItemB, icsItemA);
        await dbContext.SaveChangesAsync();

        var mediator = CreateMediatorForEmployeesAndSignatories(
            new Dictionary<Guid, EmployeeReferenceDto>
            {
                [issuedFromEmployeeId] = CreateEmployee(issuedFromEmployeeId, "Supply", "Officer", "Supply Office", "Supply", "Supply Officer", "EMP-001"),
                [receivedByEmployeeId] = CreateEmployee(receivedByEmployeeId, "End", "User", "Client Office", "Operations", "Staff", "EMP-002")
            },
            [
                new ReportSignatoryDto(Guid.NewGuid(), "RSPI", 2, "NOTED BY:", "Manager B", "Division Chief", true),
                new ReportSignatoryDto(Guid.NewGuid(), "RSPI", 1, "PREPARED BY:", "Clerk A", "Administrative Aide", true),
                new ReportSignatoryDto(Guid.NewGuid(), "RSPI", 3, "IGNORE", "Inactive", "Inactive", false)
            ]);

        var handler = new GetRSPIQueryHandler(dbContext, mediator);

        // Act
        var result = await handler.Handle(
            new GetRSPIQuery(null, null, AssetType.SE, true, 1, 20),
            CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items[0].ICSNo.ShouldBe("ICS-001");
        result.Items[0].PropertyNo.ShouldBe("PROP-001");
        result.Items[1].ICSNo.ShouldBe("ICS-002");

        result.Sections.Count.ShouldBe(2);
        result.Sections[0].ICSNo.ShouldBe("ICS-001");
        result.Sections[0].AmountTotal.ShouldBe(3000m);
        result.Sections[1].ICSNo.ShouldBe("ICS-002");
        result.Sections[1].AmountTotal.ShouldBe(2000m);

        result.PageLineCount.ShouldBe(2);
        result.PageAmountTotal.ShouldBe(5000m);
        result.OverallAmountTotal.ShouldBe(5000m);

        result.Signatories.Count.ShouldBe(2);
        result.Signatories[0].SortOrder.ShouldBe(1);
        result.Signatories[0].Label.ShouldBe("PREPARED BY:");
        result.Signatories[1].SortOrder.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_RegSpIQuery_FiltersByEmployee_AndBuildsSectionsAndTotals()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var issuedFromEmployeeId = Guid.NewGuid();
        var targetEmployeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();

        var catalog = PropertyItemCatalog.Create(TestTenantId, "PRN-001", "Printer", null, null, "Unit", 5);
        var inventoryTarget = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-10",
            AssetType.SE,
            5000m,
            "PROP-TARGET",
            catalog.Id,
            "Target Item",
            new DateOnly(2026, 1, 5),
            1,
            1500m);
        var inventoryOther = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-11",
            AssetType.SE,
            5000m,
            "PROP-OTHER",
            catalog.Id,
            "Other Item",
            new DateOnly(2026, 1, 5),
            1,
            2500m);

        var targetIcs = InventoryCustodianSlip.Create(
            TestTenantId,
            "ICS-100",
            new DateOnly(2026, 2, 1),
            AssetCategory.LowValuedSemi,
            "FC-2",
            issuedFromEmployeeId,
            targetEmployeeId);
        var otherIcs = InventoryCustodianSlip.Create(
            TestTenantId,
            "ICS-101",
            new DateOnly(2026, 2, 2),
            AssetCategory.LowValuedSemi,
            "FC-2",
            issuedFromEmployeeId,
            otherEmployeeId);

        dbContext.PropertyItemCatalog.Add(catalog);
        dbContext.TangibleInventoryItems.AddRange(inventoryTarget, inventoryOther);
        dbContext.InventoryCustodianSlips.AddRange(targetIcs, otherIcs);
        dbContext.ICSItems.AddRange(
            ICSItem.Create(TestTenantId, targetIcs.Id, inventoryTarget.Id, 1, "Target", 1500m, 3, AssetType.SE),
            ICSItem.Create(TestTenantId, otherIcs.Id, inventoryOther.Id, 1, "Other", 2500m, 3, AssetType.SE));
        await dbContext.SaveChangesAsync();

        var mediator = CreateMediatorForEmployeesAndSignatories(
            new Dictionary<Guid, EmployeeReferenceDto>
            {
                [issuedFromEmployeeId] = CreateEmployee(issuedFromEmployeeId, "Supply", "Officer", "Supply Office", "Supply", "Supply Officer", "EMP-010"),
                [targetEmployeeId] = CreateEmployee(targetEmployeeId, "Target", "User", "Target Office", "Target Dept", "Target Position", "EMP-020")
            },
            [
                new ReportSignatoryDto(Guid.NewGuid(), "RegSPI", 1, "PREPARED BY:", "Preparer", "Staff", true)
            ]);

        var handler = new GetRegSPIQueryHandler(dbContext, mediator);

        // Act
        var result = await handler.Handle(
            new GetRegSPIQuery(targetEmployeeId, AssetType.SE, ICSStatus.Active, 1, 20),
            CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].ICSNo.ShouldBe("ICS-100");
        result.Items[0].IssuedFromEmployeeName.ShouldBe("Supply Officer");

        result.EmployeeName.ShouldBe("Target User");
        result.EmployeeOfficeName.ShouldBe("Target Office");
        result.EmployeePositionName.ShouldBe("Target Position");

        result.Sections.Count.ShouldBe(1);
        result.Sections[0].ICSNo.ShouldBe("ICS-100");
        result.Sections[0].AmountTotal.ShouldBe(1500m);

        result.PageLineCount.ShouldBe(1);
        result.PageAmountTotal.ShouldBe(1500m);
        result.OverallAmountTotal.ShouldBe(1500m);

        result.Signatories.Count.ShouldBe(1);
        result.Signatories[0].Label.ShouldBe("PREPARED BY:");
    }

    [Fact]
    public async Task Handle_PTRQuery_ProjectsOfficerDisplayFields_AndOrderedItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var issuedByEmployeeId = Guid.NewGuid();
        var issuedToEmployeeId = Guid.NewGuid();
        var approvedByEmployeeId = Guid.NewGuid();
        var receivedByEmployeeId = Guid.NewGuid();

        var itemA = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-21",
            AssetType.PPE,
            5000m,
            "PPE-002",
            Guid.NewGuid(),
            "Printer A",
            new DateOnly(2026, 3, 1),
            1,
            22000m);

        var itemB = TangibleInventoryItem.Create(
            TestTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-22",
            AssetType.PPE,
            5000m,
            "PPE-001",
            Guid.NewGuid(),
            "Printer B",
            new DateOnly(2026, 3, 1),
            1,
            18000m);

        var ppeir = PPEIssuanceReport.Create(
            TestTenantId,
            "PPEIR-100",
            new DateOnly(2026, 3, 10),
            issuedToEmployeeId,
            "Main Office",
            PPEIssuanceType.TransferCO,
            issuedByEmployeeId,
            receivedByEmployeeId,
            approvedByEmployeeId);

        dbContext.TangibleInventoryItems.AddRange(itemA, itemB);
        dbContext.PPEIssuanceReports.Add(ppeir);
        dbContext.PPEIRItems.AddRange(
            PPEIRItem.Create(TestTenantId, ppeir.Id, itemA.Id, 2, "PC-2", null, "Printer A", new DateOnly(2026, 3, 1), 22000m),
            PPEIRItem.Create(TestTenantId, ppeir.Id, itemB.Id, 1, "PC-1", null, "Printer B", new DateOnly(2026, 3, 1), 18000m));
        await dbContext.SaveChangesAsync();

        var mediator = CreateMediatorForEmployeesAndSignatories(
            new Dictionary<Guid, EmployeeReferenceDto>
            {
                [issuedByEmployeeId] = CreateEmployee(issuedByEmployeeId, "Supply", "Officer", "Supply Office", "Supply", "Supply Officer", "EMP-100"),
                [issuedToEmployeeId] = CreateEmployee(issuedToEmployeeId, "Receiving", "Officer", "Receiving Office", "Ops", "Receiving Officer", "EMP-101"),
                [approvedByEmployeeId] = CreateEmployee(approvedByEmployeeId, "Approver", "Chief", "Admin Office", "Admin", "Division Chief", "EMP-102"),
                [receivedByEmployeeId] = CreateEmployee(receivedByEmployeeId, "Receiving", "Signer", "Receiving Office", "Ops", "Staff", "EMP-103")
            },
            []);

        var handler = new GetPTRQueryHandler(dbContext, mediator);

        // Act
        var result = await handler.Handle(new GetPTRQuery(ppeir.Id), CancellationToken.None);

        // Assert
        result.PTRNo.ShouldBe("PPEIR-100");
        result.FromAccountableOfficerName.ShouldBe("Supply Officer");
        result.ToAccountableOfficerName.ShouldBe("Receiving Officer");
        result.ApprovedByEmployeeName.ShouldBe("Approver Chief");
        result.ReleasedByEmployeeName.ShouldBe("Supply Officer");
        result.ReceivedByEmployeeName.ShouldBe("Receiving Signer");

        result.Items.Count.ShouldBe(2);
        result.Items[0].ItemNo.ShouldBe(1);
        result.Items[0].PropertyNumber.ShouldBe("PPE-001");
        result.Items[1].ItemNo.ShouldBe(2);
        result.Items[1].PropertyNumber.ShouldBe("PPE-002");
    }

    private static IMediator CreateMediatorForEmployeesAndSignatories(
        Dictionary<Guid, EmployeeReferenceDto> employees,
        IReadOnlyList<ReportSignatoryDto> signatories)
    {
        var mediator = Substitute.For<IMediator>();

#pragma warning disable CA2012
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetEmployeeReferenceByIdQuery>();
                employees.TryGetValue(query.Id, out var employee);
                return ValueTask.FromResult(employee);
            });

        mediator.Send(Arg.Any<GetEmployeeReferencesByIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetEmployeeReferencesByIdsQuery>();
                IReadOnlyDictionary<Guid, EmployeeReferenceDto> matched = query.Ids
                    .Where(employees.ContainsKey)
                    .Distinct()
                    .ToDictionary(id => id, id => employees[id]);

                return ValueTask.FromResult(matched);
            });

        mediator.Send(Arg.Any<GetReportSignatoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetReportSignatoriesQuery>();
                var filtered = signatories
                    .Where(x => string.Equals(x.ReportType, query.ReportType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return ValueTask.FromResult(filtered);
            });
#pragma warning restore CA2012

        return mediator;
    }

    private static EmployeeReferenceDto CreateEmployee(
        Guid id,
        string firstName,
        string lastName,
        string officeName,
        string departmentName,
        string positionName,
        string employeeNumber) =>
        new(
            id,
            employeeNumber,
            null,
            firstName,
            lastName,
            null,
            Guid.NewGuid(),
            "OFF",
            officeName,
            Guid.NewGuid(),
            "DEP",
            departmentName,
            Guid.NewGuid(),
            "POS",
            positionName,
            null,
            null,
            null,
            true,
            null);

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
        public string ApplicationName { get; set; } = nameof(ReportQueryHandlerAlignmentTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

