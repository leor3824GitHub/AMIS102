using System.Security.Claims;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmpVersions;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.RecallPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SearchPpmps;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.ProcurementPlanning;

public sealed class PpmpHandlerTests
{
    // ── CreatePpmpCommandHandler ───────────────────────────────────────────────

    [Fact]
    public async Task CreatePpmp_ValidCommand_PersistsAndReturnsDto()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var handler = new CreatePpmpCommandHandler(db, new StubCurrentUser(userId));

        var command = new CreatePpmpCommand(
            FiscalYear: 2027,
            Phase: PpmpPhase.Indicative,
            OfficeCode: "ICT",
            EndUserUnit: "ICT Unit",
            PreparedById: userId,
            Items: [ValidItem("Laptop", 80_000m)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.FiscalYear.ShouldBe(2027);
        result.Phase.ShouldBe(PpmpPhase.Indicative);
        result.Status.ShouldBe(PpmpStatus.Draft);
        result.PpmpNumber.ShouldStartWith("PPMP-2027-ICT-");
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Laptop");
    }

    [Fact]
    public async Task CreatePpmp_GeneratesSequentialNumbers_ForSameOffice()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var handler = new CreatePpmpCommandHandler(db, new StubCurrentUser(userId));
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "FIN", "Finance", userId, [ValidItem("Item", 1_000m)]);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.PpmpNumber.ShouldEndWith("001");
        second.PpmpNumber.ShouldEndWith("002");
    }

    // ── SubmitPpmpCommandHandler ───────────────────────────────────────────────

    [Fact]
    public async Task SubmitPpmp_DraftPpmp_ChangesStatusToSubmitted()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp();
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var result = await new SubmitPpmpCommandHandler(db)
            .Handle(new SubmitPpmpCommand(ppmp.Id), CancellationToken.None);

        result.Status.ShouldBe(PpmpStatus.Submitted);
    }

    [Fact]
    public async Task SubmitPpmp_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDbContext();
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await new SubmitPpmpCommandHandler(db)
                .Handle(new SubmitPpmpCommand(Guid.NewGuid()), CancellationToken.None));
    }

    // ── ApprovePpmpCommandHandler ──────────────────────────────────────────────

    [Fact]
    public async Task ApprovePpmp_SubmittedPpmp_ChangesStatusToApproved()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp();
        ppmp.Submit();
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var approvedById = Guid.NewGuid();
        var result = await new ApprovePpmpCommandHandler(db)
            .Handle(new ApprovePpmpCommand(ppmp.Id, approvedById), CancellationToken.None);

        result.Status.ShouldBe(PpmpStatus.Approved);
    }

    // ── RecallPpmpCommandHandler ───────────────────────────────────────────────

    [Fact]
    public async Task RecallPpmp_SubmittedPpmp_RevertsToDraft()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp();
        ppmp.Submit();
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var result = await new RecallPpmpCommandHandler(db)
            .Handle(new RecallPpmpCommand(ppmp.Id), CancellationToken.None);

        result.Status.ShouldBe(PpmpStatus.Draft);
    }

    // ── ReturnPpmpCommandHandler ───────────────────────────────────────────────

    [Fact]
    public async Task ReturnPpmp_SubmittedPpmp_ChangesStatusToReturned()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp();
        ppmp.Submit();
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var returnedById = Guid.NewGuid();
        var result = await new ReturnPpmpCommandHandler(db)
            .Handle(new ReturnPpmpCommand(ppmp.Id, "Needs revision", returnedById), CancellationToken.None);

        result.Status.ShouldBe(PpmpStatus.Returned);
    }

    // ── CreateUpdatePpmpCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task CreateUpdatePpmp_ApprovedPpmp_CreatesNewVersionAndSupersedes()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp(PpmpPhase.Final);
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var handler = new CreateUpdatePpmpCommandHandler(db, new StubCurrentUser(userId));

        var result = await handler.Handle(
            new CreateUpdatePpmpCommand(ppmp.Id, "Budget revision"),
            CancellationToken.None);

        result.VersionNumber.ShouldBe(1);
        result.Phase.ShouldBe(PpmpPhase.Updated);
        result.Status.ShouldBe(PpmpStatus.Draft);
        result.PreviousVersionId.ShouldBe(ppmp.Id);

        var original = await db.Ppmps.FindAsync(ppmp.Id);
        original!.IsCurrentVersion.ShouldBeFalse();
        original.Status.ShouldBe(PpmpStatus.Superseded);
    }

    // ── SearchPpmpsQueryHandler ────────────────────────────────────────────────

    [Fact]
    public async Task SearchPpmps_ByFiscalYear_ReturnsOnlyMatchingYear()
    {
        await using var db = CreateDbContext();
        var p2027 = MakeApprovedPpmp(2027, "ICT");
        var p2028 = MakeApprovedPpmp(2028, "ICT");
        db.Ppmps.AddRange(p2027, p2028);
        await db.SaveChangesAsync();

        var result = await new SearchPpmpsQueryHandler(db)
            .Handle(new SearchPpmpsQuery { FiscalYear = 2027, PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.First().FiscalYear.ShouldBe(2027);
    }

    [Fact]
    public async Task SearchPpmps_ByStatus_ReturnsOnlyMatchingStatus()
    {
        await using var db = CreateDbContext();
        var draft = MakeDraftPpmp();
        var approved = MakeApprovedPpmp(2027, "FIN");
        db.Ppmps.AddRange(draft, approved);
        await db.SaveChangesAsync();

        var result = await new SearchPpmpsQueryHandler(db)
            .Handle(new SearchPpmpsQuery { Status = PpmpStatus.Approved, PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.ShouldAllBe(x => x.Status == PpmpStatus.Approved);
    }

    [Fact]
    public async Task SearchPpmps_ByPhase_ReturnsOnlyMatchingPhase()
    {
        await using var db = CreateDbContext();
        var indicative = MakeDraftPpmp(PpmpPhase.Indicative);
        var final = MakeDraftPpmp(PpmpPhase.Final);
        db.Ppmps.AddRange(indicative, final);
        await db.SaveChangesAsync();

        var result = await new SearchPpmpsQueryHandler(db)
            .Handle(new SearchPpmpsQuery { Phase = PpmpPhase.Final, PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.ShouldAllBe(x => x.Phase == PpmpPhase.Final);
    }

    [Fact]
    public async Task SearchPpmps_CurrentVersionOnly_ExcludesSuperseded()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp(PpmpPhase.Final);
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        // Create an update (makes original non-current)
        var handler = new CreateUpdatePpmpCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));
        await handler.Handle(new CreateUpdatePpmpCommand(ppmp.Id, "Update"), CancellationToken.None);

        var result = await new SearchPpmpsQueryHandler(db)
            .Handle(new SearchPpmpsQuery { CurrentVersionOnly = true, PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.ShouldAllBe(x => x.IsCurrentVersion);
    }

    [Fact]
    public async Task SearchPpmps_Pagination_ReturnsCorrectPage()
    {
        await using var db = CreateDbContext();
        for (var i = 0; i < 5; i++)
            db.Ppmps.Add(MakeDraftPpmp());
        await db.SaveChangesAsync();

        var result = await new SearchPpmpsQueryHandler(db)
            .Handle(new SearchPpmpsQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        result.TotalCount.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
        result.PageNumber.ShouldBe(2);
    }

    // ── GetPpmpQueryHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPpmp_ExistingId_ReturnsDto()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp();
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var result = await new GetPpmpQueryHandler(db)
            .Handle(new GetPpmpQuery(ppmp.Id), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(ppmp.Id);
    }

    [Fact]
    public async Task GetPpmp_UnknownId_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDbContext();
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await new GetPpmpQueryHandler(db)
                .Handle(new GetPpmpQuery(Guid.NewGuid()), CancellationToken.None));
    }

    // ── GetPpmpVersionsQueryHandler ────────────────────────────────────────────

    [Fact]
    public async Task GetPpmpVersions_ReturnsAllVersionsInChain()
    {
        await using var db = CreateDbContext();
        var ppmp = MakeDraftPpmp(PpmpPhase.Final);
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var updateHandler = new CreateUpdatePpmpCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));
        await updateHandler.Handle(new CreateUpdatePpmpCommand(ppmp.Id, "V2"), CancellationToken.None);

        var result = await new GetPpmpVersionsQueryHandler(db)
            .Handle(new GetPpmpVersionsQuery(ppmp.VersionChainId), CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.VersionNumber == 1 && x.Phase == PpmpPhase.Final);
        result.ShouldContain(x => x.VersionNumber == 1 && x.Phase == PpmpPhase.Updated);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PpmpItemRequest ValidItem(string description, decimal budget) =>
        new(description, ProjectType.Goods, 1, "lot", "Shopping",
            false, "01/2027", "03/2027", "04/2027", "General Fund", budget, null, null);

    private static Ppmp MakeDraftPpmp(PpmpPhase phase = PpmpPhase.Indicative) =>
        Ppmp.Create(
            $"PPMP-2027-ICT-{Guid.NewGuid():N}"[..18],
            2027, phase, "ICT", "ICT Unit", Guid.NewGuid(),
            [new PpmpItemData("Item", ProjectType.Goods, 1, "lot", "Shopping",
                false, "01/2027", "03/2027", "04/2027", "General Fund", 10_000m, null, null)]);

    private static Ppmp MakeApprovedPpmp(int fiscalYear, string officeCode)
    {
        var ppmp = Ppmp.Create(
            $"PPMP-{fiscalYear}-{officeCode}-{Guid.NewGuid():N}"[..18],
            fiscalYear, PpmpPhase.Final, officeCode, $"{officeCode} Unit", Guid.NewGuid(),
            [new PpmpItemData("Item", ProjectType.Goods, 1, "lot", "Shopping",
                false, "01/2027", "03/2027", "04/2027", "General Fund", 10_000m, null, null)]);
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        return ppmp;
    }

    private static ProcurementPlanningDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementPlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new AppTenantInfo("root", "root", "Root Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var accessor = new StubTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "Generic.Tests"
        });
        return new ProcurementPlanningDbContext(accessor, options, dbOptions, new StubHostEnvironment());
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public string? Name => "Test";
        public Guid GetUserId() => userId;
        public string? GetUserEmail() => "test@example.com";
        public string? GetTenant() => "root";
        public bool IsAuthenticated() => true;
        public bool IsInRole(string role) => false;
        public IEnumerable<Claim>? GetUserClaims() => [];
    }

    private sealed class StubTenantAccessor(IMultiTenantContext<AppTenantInfo> ctx)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = ctx;
        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(PpmpHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
