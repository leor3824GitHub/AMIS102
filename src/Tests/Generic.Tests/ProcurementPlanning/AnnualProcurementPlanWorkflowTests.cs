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
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.PromoteToFinalPpmp;
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
    public async Task GetAnnualProcurementPlanQueryHandler_WhenPublished_ReturnsStoredAppLineItems()
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
    public async Task GetAnnualProcurementPlanQueryHandler_WhenApproved_ReturnsStoredAppLineItems()
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
    public async Task CreateUpdateAppCommandHandler_WhenAppIsPublished_ClonesAppLineItemsIntoNewVersion()
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
        var handler = new CreateUpdateAppCommandHandler(dbContext, currentUser);

        // Act
        var result = await handler.Handle(
            new CreateUpdateAppCommand(app.Id, "Need revised quantities"),
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

        var originalLines = await dbContext.AppLineItems
            .Where(x => x.AppId == app.Id)
            .OrderBy(x => x.ItemNo)
            .ToListAsync();

        var amendmentLines = await dbContext.AppLineItems
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

        var amendment = originalApp.CreateUpdate("Annual adjustment", Guid.NewGuid());
        amendment.CreatedBy = "amender";
        originalApp.Supersede();

        var deletedApp = AnnualProcurementPlan.Create("APP-DELETED", 2026, AppPhase.Indicative);
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

        var amendment = originalApp.CreateUpdate("Updated requirement", Guid.NewGuid());
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

    // ── New Workflow Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task ConsolidatePpmps_PopulatesSourcePpmpsAndDenormalizedLineItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Server Rack", 250000m);
        dbContext.Ppmps.Add(ppmp);

        var app = AnnualProcurementPlan.Create("APP-001", 2026, AppPhase.Final);
        app.CreatedBy = "test";
        var consolidatorId = Guid.NewGuid();
        app.ConsolidatePpmps([ppmp], consolidatorId);

        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        // Assert SourcePpmps
        var sourcePpmps = await dbContext.AppSourcePpmps
            .Where(x => x.AppId == app.Id)
            .ToListAsync();
        sourcePpmps.Count.ShouldBe(1);
        sourcePpmps[0].PpmpId.ShouldBe(ppmp.Id);
        sourcePpmps[0].OfficeCode.ShouldBe(ppmp.OfficeCode);
        sourcePpmps[0].Phase.ShouldBe(PpmpPhase.Final);

        // Assert LineItems (denormalized)
        var lineItems = await dbContext.AppLineItems
            .Where(x => x.AppId == app.Id)
            .OrderBy(x => x.ItemNo)
            .ToListAsync();
        lineItems.Count.ShouldBe(1);
        lineItems[0].SourcePpmpId.ShouldBe(ppmp.Id);
        lineItems[0].OfficeCode.ShouldBe("ICT");
        lineItems[0].GeneralDescription.ShouldBe("Server Rack");
        lineItems[0].EstimatedBudget.ShouldBe(250000m);
        lineItems[0].ItemNo.ShouldBe(1);

        // Assert ConsolidatedById set on APP
        var savedApp = await dbContext.AnnualProcurementPlans.FindAsync(app.Id);
        savedApp!.ConsolidatedById.ShouldBe(consolidatorId);
    }

    [Fact]
    public void ConsolidatePpmps_PhaseGuard_ThrowsWhenPpmpPhaseDoesNotMatchApp()
    {
        // Arrange — Indicative PPMP, Final APP
        var indicativePpmp = CreatePpmpInPhase(PpmpPhase.Indicative, "Keyboard", 5000m);
        indicativePpmp.Approve(Guid.NewGuid());

        var finalApp = AnnualProcurementPlan.Create("APP-002", 2026, AppPhase.Final);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            finalApp.ConsolidatePpmps([indicativePpmp], Guid.NewGuid()));
        ex.Message.ShouldContain("phase");
    }

    [Fact]
    public void ConsolidatePpmps_StatusGuard_ThrowsWhenAppIsNotDraftOrReturned()
    {
        // Arrange
        var ppmp = CreatePpmpInPhase(PpmpPhase.Final, "Monitor", 35000m);
        ppmp.Approve(Guid.NewGuid());
        var app = AnnualProcurementPlan.Create("APP-003", 2026, AppPhase.Final);
        app.ConsolidatePpmps([ppmp], Guid.NewGuid());
        app.Publish();

        // Act & Assert — cannot re-consolidate into a Published APP
        var ex = Should.Throw<InvalidOperationException>(() =>
            app.ConsolidatePpmps([ppmp], Guid.NewGuid()));
        ex.Message.ShouldContain("Draft or Returned");
    }

    [Fact]
    public void ConsolidatePpmps_Idempotent_ReconsolidatingSamePpmpReplacesItsItems()
    {
        // Arrange — domain-level idempotency: calling ConsolidatePpmps twice with the same PPMP
        // must result in exactly one SourcePpmp and one LineItem (no duplicates).
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Printer", 30000m);
        var app = AnnualProcurementPlan.Create("APP-004", 2026, AppPhase.Final);
        app.CreatedBy = "test";

        // First consolidation
        var firstConsolidatorId = Guid.NewGuid();
        app.ConsolidatePpmps([ppmp], firstConsolidatorId);
        app.SourcePpmps.Count.ShouldBe(1);
        app.LineItems.Count.ShouldBe(1);

        // Second consolidation (same PPMP — idempotent replacement)
        var secondConsolidatorId = Guid.NewGuid();
        app.ConsolidatePpmps([ppmp], secondConsolidatorId);

        // Assert — still exactly one SourcePpmp and one LineItem (no duplicates)
        app.SourcePpmps.Count.ShouldBe(1);
        app.LineItems.Count.ShouldBe(1);
        app.SourcePpmps[0].IncludedById.ShouldBe(secondConsolidatorId);
        app.LineItems[0].GeneralDescription.ShouldBe("Printer");
    }

    [Fact]
    public async Task PromoteToFinalApp_CreatesNewFinalAppDraft_WithNoItems()
    {
        // Arrange — Indicative APP is Approved
        await using var dbContext = CreateDbContext();
        var ppmp = CreatePpmpInPhase(PpmpPhase.Indicative, "Laptop", 80000m);
        ppmp.Approve(Guid.NewGuid());

        var indicativeApp = AnnualProcurementPlan.Create("APP-005", 2026, AppPhase.Indicative);
        indicativeApp.CreatedBy = "test";
        indicativeApp.ConsolidatePpmps([ppmp], Guid.NewGuid());
        indicativeApp.Publish();
        indicativeApp.Approve(Guid.NewGuid());

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(indicativeApp);
        await dbContext.SaveChangesAsync();

        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var handler = new PromoteToFinalAppCommandHandler(dbContext, currentUser);

        // Act
        var result = await handler.Handle(new PromoteToFinalAppCommand(indicativeApp.Id), CancellationToken.None);

        // Assert
        result.Phase.ShouldBe(AppPhase.Final);
        result.Status.ShouldBe(AppStatus.Draft);
        result.VersionNumber.ShouldBe(1);
        result.Items.Count.ShouldBe(0);        // Final APP starts empty
        result.PreviousVersionId.ShouldBe(indicativeApp.Id);
        result.TotalEstimatedBudget.ShouldBe(0m);
        result.VersionChainId.ShouldBe(indicativeApp.VersionChainId);

        var original = await dbContext.AnnualProcurementPlans.FindAsync(indicativeApp.Id);
        original.ShouldNotBeNull();
        original!.IsCurrentVersion.ShouldBeFalse();
        original.Status.ShouldBe(AppStatus.Superseded);
    }

    [Fact]
    public async Task FullFinalWorkflow_PromotePpmpToFinal_ConsolidateIntoFinalApp_ThenApprove()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var currentUser = new TestCurrentUser(Guid.NewGuid());

        // Step 1: Create and approve an Indicative PPMP
        var indicativePpmp = CreatePpmpInPhase(PpmpPhase.Indicative, "UPS Battery", 55000m);
        indicativePpmp.Approve(Guid.NewGuid());
        dbContext.Ppmps.Add(indicativePpmp);
        await dbContext.SaveChangesAsync();

        // Step 2: Promote to Final PPMP
        var promoteHandler = new PromoteToFinalPpmpCommandHandler(dbContext, currentUser);
        var finalPpmpDto = await promoteHandler.Handle(
            new PromoteToFinalPpmpCommand(indicativePpmp.Id), CancellationToken.None);
        finalPpmpDto.Phase.ShouldBe(PpmpPhase.Final);
        finalPpmpDto.Status.ShouldBe(PpmpStatus.Draft);
        finalPpmpDto.VersionChainId.ShouldBe(indicativePpmp.VersionChainId);

        var supersededIndicative = await dbContext.Ppmps.FindAsync(indicativePpmp.Id);
        supersededIndicative.ShouldNotBeNull();
        supersededIndicative!.IsCurrentVersion.ShouldBeFalse();
        supersededIndicative.Status.ShouldBe(PpmpStatus.Superseded);

        // Step 3: Approve the Final PPMP
        var finalPpmpEntity = await dbContext.Ppmps
            .Include(x => x.Items)
            .SingleAsync(x => x.Id == finalPpmpDto.Id);
        finalPpmpEntity.Submit();
        finalPpmpEntity.Approve(Guid.NewGuid());
        await dbContext.SaveChangesAsync();

        // Step 4: Create a Final APP and consolidate the Final PPMP
        var finalApp = AnnualProcurementPlan.Create("APP-006", 2026, AppPhase.Final);
        finalApp.CreatedBy = "test";

        var finalPpmpForConsolidate = await dbContext.Ppmps
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleAsync(x => x.Id == finalPpmpDto.Id);
        finalApp.ConsolidatePpmps([finalPpmpForConsolidate], currentUser.GetUserId());
        dbContext.AnnualProcurementPlans.Add(finalApp);
        await dbContext.SaveChangesAsync();

        // Step 5: Publish and approve the Final APP
        var publishHandler = new PublishAnnualProcurementPlanCommandHandler(dbContext, currentUser);
        await publishHandler.Handle(new PublishAnnualProcurementPlanCommand(finalApp.Id), CancellationToken.None);

        var approveHandler = new ApproveAnnualProcurementPlanCommandHandler(dbContext);
        await approveHandler.Handle(new ApproveAppCommand(finalApp.Id, currentUser.GetUserId()), CancellationToken.None);

        // Assert
        var approved = await dbContext.AnnualProcurementPlans.FindAsync(finalApp.Id);
        approved!.Phase.ShouldBe(AppPhase.Final);
        approved.Status.ShouldBe(AppStatus.Approved);

        var lineItems = await dbContext.AppLineItems.Where(x => x.AppId == finalApp.Id).ToListAsync();
        lineItems.Count.ShouldBe(1);
        lineItems[0].GeneralDescription.ShouldBe("UPS Battery");
        lineItems[0].EstimatedBudget.ShouldBe(55000m);
    }

    [Fact]
    public async Task CreateUpdateApp_ClonesSourcePpmpsAndLineItems_IntoUpdatedVersion()
    {
        // Arrange — Approved Final APP
        await using var dbContext = CreateDbContext();
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Network Cable", 12000m);
        var finalApp = AnnualProcurementPlan.Create("APP-007", 2026, AppPhase.Final);
        finalApp.CreatedBy = "test";
        finalApp.ConsolidatePpmps([ppmp], Guid.NewGuid());
        finalApp.Publish();
        finalApp.Approve(Guid.NewGuid());

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(finalApp);
        await dbContext.SaveChangesAsync();

        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var handler = new CreateUpdateAppCommandHandler(dbContext, currentUser);

        // Act
        var updatedDto = await handler.Handle(
            new CreateUpdateAppCommand(finalApp.Id, "Budget revision"),
            CancellationToken.None);

        // Assert shape
        updatedDto.Phase.ShouldBe(AppPhase.Updated);
        updatedDto.Status.ShouldBe(AppStatus.Draft);
        updatedDto.VersionNumber.ShouldBe(2);
        updatedDto.PreviousVersionId.ShouldBe(finalApp.Id);
        updatedDto.AmendmentReason.ShouldBe("Budget revision");

        // Assert cloned LineItems
        updatedDto.Items.Count.ShouldBe(1);
        updatedDto.Items[0].GeneralDescription.ShouldBe("Network Cable");
        updatedDto.Items[0].EstimatedBudget.ShouldBe(12000m);

        // Assert cloned SourcePpmps in DB
        var clonedSources = await dbContext.AppSourcePpmps
            .Where(x => x.AppId == updatedDto.Id)
            .ToListAsync();
        clonedSources.Count.ShouldBe(1);
        clonedSources[0].PpmpId.ShouldBe(ppmp.Id);

        // Assert original was superseded
        var original = await dbContext.AnnualProcurementPlans.FindAsync(finalApp.Id);
        original!.Status.ShouldBe(AppStatus.Superseded);
        original.IsCurrentVersion.ShouldBeFalse();
    }

    [Fact]
    public async Task AppItemsAreImmutableAfterPpmpMutation_DenormalizedCopyNotAffected()
    {
        // Arrange — consolidate PPMP into APP, then mutate the PPMP item
        await using var dbContext = CreateDbContext();
        var ppmp = CreateApprovedPpmp(Guid.NewGuid(), Guid.NewGuid(), "Original Item", 60000m);
        var app = AnnualProcurementPlan.Create("APP-008", 2026, AppPhase.Final);
        app.CreatedBy = "test";
        app.ConsolidatePpmps([ppmp], Guid.NewGuid());

        dbContext.Ppmps.Add(ppmp);
        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync();

        // Mutate the source PPMP item
        var ppmpItem = await dbContext.PpmpItems.SingleAsync();
        dbContext.Entry(ppmpItem).Property(x => x.GeneralDescription).CurrentValue = "Mutated After Consolidation";
        dbContext.Entry(ppmpItem).Property(x => x.EstimatedBudget).CurrentValue = 999m;
        await dbContext.SaveChangesAsync();

        // Assert — APP line item is unchanged (denormalized copy)
        var lineItems = await dbContext.AppLineItems.Where(x => x.AppId == app.Id).ToListAsync();
        lineItems.Count.ShouldBe(1);
        lineItems[0].GeneralDescription.ShouldBe("Original Item");
        lineItems[0].EstimatedBudget.ShouldBe(60000m);
    }

    private static Ppmp CreatePpmpInPhase(PpmpPhase phase, string description, decimal budget)
    {
        var ppmp = Ppmp.Create(
            ppmpNumber: $"PPMP-{Guid.NewGuid():N}"[..13],
            fiscalYear: 2026,
            phase: phase,
            officeCode: "ICT",
            endUserUnit: "ICT Unit",
            preparedById: Guid.NewGuid(),
            items:
            [
                new PpmpItemData(
                    description,
                    ProjectType.Goods,
                    1,
                    "lot",
                    "Shopping",
                    false,
                    "Jan",
                    "Feb",
                    "Mar",
                    "General Fund",
                    budget,
                    null,
                    "seed")
            ]);
        ppmp.Submit();
        return ppmp;
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
            phase: PpmpPhase.Final,
            officeCode: "ICT",
            endUserUnit: "ICT Unit",
            preparedById: preparedById,
            items:
            [
                new PpmpItemData(
                    description,
                    ProjectType.Goods,
                    1,
                    "lot",
                    "Shopping",
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
        var app = AnnualProcurementPlan.Create($"APP-{Guid.NewGuid():N}"[..12], 2026, AppPhase.Final);
        app.CreatedBy = userId;
        app.ConsolidatePpmps([ppmp], Guid.NewGuid());
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