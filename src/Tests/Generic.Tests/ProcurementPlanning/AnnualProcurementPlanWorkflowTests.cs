using System.Security.Claims;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.ProcurementPlanning;

public sealed class AnnualProcurementPlanWorkflowTests
{
    [Fact]
    public async Task GetAnnualProcurementPlanQueryHandler_WhenAppIsDraft_ReturnsLiveCanonicalItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var preparedById = Guid.NewGuid();
        var approvedById = Guid.NewGuid();
        var ppmp = CreateApprovedPpmp(preparedById, approvedById, "Laptop", 125000m);
        var app = CreateDraftAppFrom(ppmp, "draft-user");

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        var handler = new GetAnnualProcurementPlanQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(new GetAnnualProcurementPlanQuery(app.Id), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(AppStatus.Draft);
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Laptop");
        result.Items[0].OfficeCode.ShouldBe(ppmp.OfficeCode);
        result.TotalEstimatedBudget.ShouldBe(125000m);
    }

    [Fact]
    public async Task GetAnnualProcurementPlanQueryHandler_WhenPublishedSnapshotExists_ReturnsSnapshotItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var preparedById = Guid.NewGuid();
        var approvedById = Guid.NewGuid();
        var ppmp = CreateApprovedPpmp(preparedById, approvedById, "Original Laptop", 125000m);
        var app = CreateDraftAppFrom(ppmp, "draft-user");

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        var publishHandler = new PublishAnnualProcurementPlanCommandHandler(dbContext, new TestCurrentUser(Guid.NewGuid()));
        await publishHandler.Handle(new PublishAnnualProcurementPlanCommand(app.Id), CancellationToken.None);

        var ppmpItem = await dbContext.PpmpItems.SingleAsync();
        dbContext.Entry(ppmpItem).Property(x => x.GeneralDescription).CurrentValue = "Mutated Live Item";
        dbContext.Entry(ppmpItem).Property(x => x.EstimatedBudget).CurrentValue = 999999m;
        await dbContext.SaveChangesAsync();

        var queryHandler = new GetAnnualProcurementPlanQueryHandler(dbContext);

        // Act
        var result = await queryHandler.Handle(new GetAnnualProcurementPlanQuery(app.Id), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(AppStatus.Published);
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Original Laptop");
        result.Items[0].EstimatedBudget.ShouldBe(125000m);
        result.TotalEstimatedBudget.ShouldBe(125000m);
    }

    [Fact]
    public async Task GetAnnualProcurementPlanQueryHandler_WhenApprovedSnapshotExists_ReturnsApprovedSnapshotItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var preparedById = Guid.NewGuid();
        var approvedById = Guid.NewGuid();
        var ppmp = CreateApprovedPpmp(preparedById, approvedById, "Approved Printer", 78000m);
        var app = CreateDraftAppFrom(ppmp, "draft-user");

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        var publishHandler = new PublishAnnualProcurementPlanCommandHandler(dbContext, new TestCurrentUser(Guid.NewGuid()));
        await publishHandler.Handle(new PublishAnnualProcurementPlanCommand(app.Id), CancellationToken.None);

        var approveHandler = new ApproveAnnualProcurementPlanCommandHandler(dbContext);
        await approveHandler.Handle(new ApproveAppCommand(app.Id, Guid.NewGuid()), CancellationToken.None);

        var ppmpItem = await dbContext.PpmpItems.SingleAsync();
        dbContext.Entry(ppmpItem).Property(x => x.GeneralDescription).CurrentValue = "Mutated After Approval";
        dbContext.Entry(ppmpItem).Property(x => x.EstimatedBudget).CurrentValue = 123m;
        await dbContext.SaveChangesAsync();

        var queryHandler = new GetAnnualProcurementPlanQueryHandler(dbContext);

        // Act
        var result = await queryHandler.Handle(new GetAnnualProcurementPlanQuery(app.Id), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(AppStatus.Approved);
        result.ApprovedById.ShouldNotBeNull();
        result.ApprovedOn.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Approved Printer");
        result.Items[0].EstimatedBudget.ShouldBe(78000m);
        result.TotalEstimatedBudget.ShouldBe(78000m);
    }

    [Fact]
    public async Task AmendAnnualProcurementPlanCommandHandler_WhenAppIsPublished_ClonesLineReferencesIntoNewVersion()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var preparedById = Guid.NewGuid();
        var approvedById = Guid.NewGuid();
        var ppmp = CreateApprovedPpmp(preparedById, approvedById, "Network Switch", 64000m);
        var app = CreateDraftAppFrom(ppmp, "draft-user");
        app.Publish();

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var handler = new AmendAnnualProcurementPlanCommandHandler(dbContext, currentUser);

        // Act
        var result = await handler.Handle(
            new AmendAnnualProcurementPlanCommand(app.Id, "Need revised quantities", AppRevisionType.Revised),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(AppStatus.Draft);
        result.VersionNumber.ShouldBe(2);
        result.PreviousVersionId.ShouldBe(app.Id);
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Network Switch");

        var storedApps = await dbContext.AnnualProcurementPlans
            .IgnoreQueryFilters()
            .OrderBy(x => x.VersionNumber)
            .ToListAsync();

        storedApps.Count.ShouldBe(2);
        storedApps[0].Status.ShouldBe(AppStatus.Superseded);
        storedApps[0].IsCurrentVersion.ShouldBeFalse();
        storedApps[1].Status.ShouldBe(AppStatus.Draft);
        storedApps[1].IsCurrentVersion.ShouldBeTrue();

        var originalLines = await dbContext.AppLineReferences
            .Where(x => x.AppId == app.Id)
            .OrderBy(x => x.ItemNo)
            .ToListAsync();

        var amendmentLines = await dbContext.AppLineReferences
            .Where(x => x.AppId == result.Id)
            .OrderBy(x => x.ItemNo)
            .ToListAsync();

        amendmentLines.Count.ShouldBe(originalLines.Count);
        amendmentLines.Select(x => x.SourcePpmpItemId).ShouldBe(originalLines.Select(x => x.SourcePpmpItemId));
        amendmentLines.Select(x => x.ItemNo).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task SearchAnnualProcurementPlansQueryHandler_CurrentVersionOnly_ExcludesSupersededAndDeletedApps()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Searchable Item", 45000m);
        var originalApp = CreateDraftAppFrom(ppmp, "draft-user");
        originalApp.Publish();

        var amendment = originalApp.CreateAmendment("Annual adjustment", AppRevisionType.Revised, Guid.NewGuid().ToString());
        amendment.CreatedBy = "amender";
        originalApp.Supersede();

        var deletedApp = AnnualProcurementPlan.Create("APP-DELETED", 2026, AppRevisionType.Original);
        deletedApp.IsDeleted = true;
        deletedApp.CreatedBy = "deleted-user";

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.AddRange(originalApp, amendment, deletedApp);
        await dbContext.SaveChangesAsync();

        var handler = new SearchAnnualProcurementPlansQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(
            new SearchAnnualProcurementPlansQuery
            {
                Keyword = originalApp.AppNumber,
                CurrentVersionOnly = true,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);
        var items = result.Items.ToList();

        // Assert
        result.TotalCount.ShouldBe(1);
        items.Count.ShouldBe(1);
        items[0].Id.ShouldBe(amendment.Id);
        items[0].Status.ShouldBe(AppStatus.Draft);
        items[0].VersionNumber.ShouldBe(2);
        items[0].ItemCount.ShouldBe(1);
        items[0].TotalEstimatedBudget.ShouldBe(45000m);
    }

    [Fact]
    public async Task GetAppVersionsQueryHandler_ReturnsOrderedVersionsWithAggregatedTotals()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Versioned Item", 88000m);
        var originalApp = CreateDraftAppFrom(ppmp, "draft-user");
        originalApp.Publish();

        var amendment = originalApp.CreateAmendment("Updated requirement", AppRevisionType.Revised, Guid.NewGuid().ToString());
        amendment.CreatedBy = "amender";
        originalApp.Supersede();

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.AddRange(originalApp, amendment);
        await dbContext.SaveChangesAsync();

        var handler = new GetAppVersionsQueryHandler(dbContext);

        // Act
        var result = (await handler.Handle(new GetAppVersionsQuery(originalApp.VersionChainId), CancellationToken.None)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(originalApp.Id);
        result[0].VersionNumber.ShouldBe(1);
        result[0].Status.ShouldBe(AppStatus.Superseded);
        result[0].ItemCount.ShouldBe(1);
        result[0].TotalEstimatedBudget.ShouldBe(88000m);

        result[1].Id.ShouldBe(amendment.Id);
        result[1].VersionNumber.ShouldBe(2);
        result[1].Status.ShouldBe(AppStatus.Draft);
        result[1].ItemCount.ShouldBe(1);
        result[1].TotalEstimatedBudget.ShouldBe(88000m);
    }

    private static ProcurementPlanningDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementPlanningDbContext>()
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

        return new ProcurementPlanningDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private static Ppmp CreateApprovedPpmp(Guid preparedById, Guid approvedById, string description, decimal estimatedBudget)
    {
        var ppmp = Ppmp.Create(
            ppmpNumber: $"PPMP-{Guid.NewGuid():N}"[..13],
            fiscalYear: 2026,
            ppmpType: PpmpType.Final,
            officeCode: "ICT",
            endUserUnit: "ICT Unit",
            preparedById: preparedById,
            items:
            [
                new PpmpItemRequest(
                    description,
                    ProjectType.Goods,
                    1,
                    "lot",
                    ModeOfProcurement.Shopping,
                    false,
                    "Jan",
                    "Feb",
                    "Mar",
                    "General Fund",
                    estimatedBudget,
                    null,
                    "seed")
            ]);

        ppmp.Submit();
        ppmp.Approve(approvedById);
        return ppmp;
    }

    private static AnnualProcurementPlan CreateDraftAppFrom(Ppmp ppmp, string userId)
    {
        var app = AnnualProcurementPlan.Create($"APP-{Guid.NewGuid():N}"[..12], 2026, AppRevisionType.Original);
        app.CreatedBy = userId;
        app.ConsolidatePpmps([ppmp], userId);
        return app;
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
        public string ApplicationName { get; set; } = nameof(AnnualProcurementPlanWorkflowTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public string? Name => "Test User";

        public Guid GetUserId() => userId;

        public string? GetUserEmail() => "test@example.com";

        public string? GetTenant() => "test-tenant";

        public bool IsAuthenticated() => true;

        public bool IsInRole(string role) => false;

        public IEnumerable<Claim>? GetUserClaims() => [];
    }
}