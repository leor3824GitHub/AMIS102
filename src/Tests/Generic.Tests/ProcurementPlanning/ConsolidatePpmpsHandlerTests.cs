using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAvailablePpmps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.ProcurementPlanning;

public sealed class ConsolidatePpmpsHandlerTests
{
    // ── ConsolidatePpmps handler ───────────────────────────────────────────────

    [Fact]
    public async Task ConsolidatePpmps_WithApprovedPpmp_SavesSourcePpmpAndLineItems()
    {
        // Arrange
        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;
        var userId = Guid.NewGuid();
        var ppmp = MakeApprovedPpmp(PpmpPhase.Indicative, "Laptop", 80_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-001", 2027, AppPhase.Indicative);
        app.CreatedBy = "seed";
        seedDb.Ppmps.Add(ppmp);
        seedDb.AnnualProcurementPlans.Add(app);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(userId));

        // Act
        var result = await handler.Handle(
            new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]),
            CancellationToken.None);

        // Assert — DTO shape
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Laptop");
        result.Items[0].EstimatedBudget.ShouldBe(80_000m);
        result.TotalEstimatedBudget.ShouldBe(80_000m);
        result.ConsolidatedById.ShouldBe(userId);

        // Assert — DB side effects
        var sourcePpmps = await db.AppSourcePpmps.Where(x => x.AppId == app.Id).ToListAsync();
        sourcePpmps.Count.ShouldBe(1);
        sourcePpmps[0].PpmpId.ShouldBe(ppmp.Id);

        var lineItems = await db.AppLineItems.Where(x => x.AppId == app.Id).ToListAsync();
        lineItems.Count.ShouldBe(1);

        // Assert — PPMP status changed to Consolidated
        var savedPpmp = await db.Ppmps.FindAsync(ppmp.Id);
        savedPpmp!.Status.ShouldBe(PpmpStatus.Consolidated);
    }

    [Fact]
    public async Task ConsolidatePpmps_ReConsolidate_ReplacesItemsWithoutDuplicates()
    {
        // Arrange
        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;
        var userId = Guid.NewGuid();
        var ppmp = MakeApprovedPpmp(PpmpPhase.Final, "Printer", 30_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-002", 2027, AppPhase.Final);
        app.CreatedBy = "seed";
        seedDb.Ppmps.Add(ppmp);
        seedDb.AnnualProcurementPlans.Add(app);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(userId));

        // First consolidation
        await handler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None);

        // Second consolidation of same PPMP (idempotent)
        var result = await handler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None);

        // Assert — still exactly 1 source PPMP and 1 line item (no duplicates)
        result.Items.Count.ShouldBe(1);

        var sourcePpmps = await db.AppSourcePpmps.Where(x => x.AppId == app.Id).ToListAsync();
        sourcePpmps.Count.ShouldBe(1);

        var lineItems = await db.AppLineItems.Where(x => x.AppId == app.Id).ToListAsync();
        lineItems.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConsolidatePpmps_PhaseMismatch_ThrowsCustomException()
    {
        // Arrange — Indicative PPMP vs Final APP
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;
        var ppmp = MakeApprovedPpmp(PpmpPhase.Indicative, "Monitor", 25_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-003", 2027, AppPhase.Final);
        app.CreatedBy = "seed";
        db.Ppmps.Add(ppmp);
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        // Act & Assert
        await Should.ThrowAsync<CustomException>(async () =>
            await handler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None));
    }

    [Fact]
    public async Task ConsolidatePpmps_EmptyPpmpIds_ThrowsCustomException()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;
        var app = AnnualProcurementPlan.Create("APP-2027-004", 2027, AppPhase.Indicative);
        app.CreatedBy = "seed";
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        await Should.ThrowAsync<CustomException>(async () =>
            await handler.Handle(new ConsolidatePpmpsCommand(app.Id, []), CancellationToken.None));
    }

    [Fact]
    public async Task ConsolidatePpmps_UnknownAppId_ThrowsCustomException()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;
        var ppmp = MakeApprovedPpmp(PpmpPhase.Indicative, "Router", 10_000m);
        db.Ppmps.Add(ppmp);
        await db.SaveChangesAsync();

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        await Should.ThrowAsync<CustomException>(async () =>
            await handler.Handle(new ConsolidatePpmpsCommand(Guid.NewGuid(), [ppmp.Id]), CancellationToken.None));
    }

    [Fact]
    public async Task ConsolidatePpmps_UnapprovedPpmp_ThrowsConflict()
    {
        // Arrange — PPMP is still Draft, not Approved
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;
        var ppmp = MakeDraftPpmp(PpmpPhase.Indicative, "Switch", 15_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-005", 2027, AppPhase.Indicative);
        app.CreatedBy = "seed";
        db.Ppmps.Add(ppmp);
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        await Should.ThrowAsync<CustomException>(async () =>
            await handler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None));
    }

    // ── GetAvailablePpmps handler ──────────────────────────────────────────────

    [Fact]
    public async Task GetAvailablePpmps_ReturnsOnlyApprovedPpmpsMatchingAppPhase()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var indicativeApproved = MakeApprovedPpmp(PpmpPhase.Indicative, "Keyboard", 5_000m);
        var finalApproved = MakeApprovedPpmp(PpmpPhase.Final, "Mouse", 3_000m);
        var indicativeDraft = MakeDraftPpmp(PpmpPhase.Indicative, "USB Hub", 2_000m);
        db.Ppmps.AddRange(indicativeApproved, finalApproved, indicativeDraft);

        var app = AnnualProcurementPlan.Create("APP-2027-010", 2027, AppPhase.Indicative);
        app.CreatedBy = "seed";
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new GetAvailablePpmpsQueryHandler(db);

        // Act
        var result = await handler.Handle(
            new GetAvailablePpmpsForAppQuery(FiscalYear: 2027, AppId: app.Id),
            CancellationToken.None);

        // Assert — only the Indicative+Approved PPMP appears
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(indicativeApproved.Id);
    }

    [Fact]
    public async Task GetAvailablePpmps_WithoutAppId_ReturnsAllApproved()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var p1 = MakeApprovedPpmp(PpmpPhase.Indicative, "P1", 1_000m);
        var p2 = MakeApprovedPpmp(PpmpPhase.Final, "P2", 2_000m);
        db.Ppmps.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var handler = new GetAvailablePpmpsQueryHandler(db);

        var result = await handler.Handle(
            new GetAvailablePpmpsForAppQuery(FiscalYear: 2027, AppId: null),
            CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAvailablePpmps_AlreadyConsolidatedPpmpInThisApp_IsIncluded()
    {
        // A PPMP that was already consolidated into this APP should still appear (locked, re-selectable)
        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;
        var userId = Guid.NewGuid();

        var ppmp = MakeApprovedPpmp(PpmpPhase.Indicative, "Server", 200_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-011", 2027, AppPhase.Indicative);
        app.CreatedBy = "seed";
        seedDb.Ppmps.Add(ppmp);
        seedDb.AnnualProcurementPlans.Add(app);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;

        // Consolidate once so PPMP becomes Consolidated
        var consolidateHandler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(userId));
        await consolidateHandler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None);

        var queryHandler = new GetAvailablePpmpsQueryHandler(db);

        var result = await queryHandler.Handle(
            new GetAvailablePpmpsForAppQuery(FiscalYear: 2027, AppId: app.Id),
            CancellationToken.None);

        // The already-consolidated PPMP must still appear so the user can see it (locked in UI)
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(ppmp.Id);
        result[0].Status.ShouldBe(PpmpStatus.Consolidated);
    }

    // ── Updated-APP phase consolidation rules ─────────────────────────────────

    [Fact]
    public async Task ConsolidatePpmps_UpdatedApp_AcceptsFinalPpmp_Succeeds()
    {
        // Updated APP may consolidate Final-phase PPMPs
        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;

        var ppmp = MakeApprovedPpmp(PpmpPhase.Final, "Scanner", 40_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-UP1", 2027, AppPhase.Updated);
        app.CreatedBy = "seed";
        seedDb.Ppmps.Add(ppmp);
        seedDb.AnnualProcurementPlans.Add(app);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;
        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(
            new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Scanner");
    }

    [Fact]
    public async Task ConsolidatePpmps_UpdatedApp_AcceptsUpdatedPpmp_Succeeds()
    {
        // Updated APP may also consolidate Updated-phase PPMPs
        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;

        var ppmp = MakeApprovedPpmp(PpmpPhase.Updated, "Projector", 55_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-UP2", 2027, AppPhase.Updated);
        app.CreatedBy = "seed";
        seedDb.Ppmps.Add(ppmp);
        seedDb.AnnualProcurementPlans.Add(app);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;
        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(
            new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].GeneralDescription.ShouldBe("Projector");
    }

    [Fact]
    public async Task ConsolidatePpmps_UpdatedApp_IndicativePpmpMismatch_ThrowsCustomException()
    {
        // Updated APP must reject Indicative-phase PPMPs
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var ppmp = MakeApprovedPpmp(PpmpPhase.Indicative, "Webcam", 8_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-UP3", 2027, AppPhase.Updated);
        app.CreatedBy = "seed";
        db.Ppmps.Add(ppmp);
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new ConsolidatePpmpsCommandHandler(db, new StubCurrentUser(Guid.NewGuid()));

        await Should.ThrowAsync<CustomException>(async () =>
            await handler.Handle(new ConsolidatePpmpsCommand(app.Id, [ppmp.Id]), CancellationToken.None));
    }

    [Fact]
    public async Task GetAvailablePpmps_UpdatedApp_ReturnsBothFinalAndUpdatedPpmps_NotIndicative()
    {
        // GetAvailablePpmps with Updated APP must return Final AND Updated PPMPs, but not Indicative
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var indicative = MakeApprovedPpmp(PpmpPhase.Indicative, "Headset", 3_000m);
        var final = MakeApprovedPpmp(PpmpPhase.Final, "Docking Station", 12_000m);
        var updated = MakeApprovedPpmp(PpmpPhase.Updated, "External SSD", 9_000m);
        var app = AnnualProcurementPlan.Create("APP-2027-UP4", 2027, AppPhase.Updated);
        app.CreatedBy = "seed";
        db.Ppmps.AddRange(indicative, final, updated);
        db.AnnualProcurementPlans.Add(app);
        await db.SaveChangesAsync();

        var handler = new GetAvailablePpmpsQueryHandler(db);

        var result = await handler.Handle(
            new GetAvailablePpmpsForAppQuery(FiscalYear: 2027, AppId: app.Id),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.Id == final.Id);
        result.ShouldContain(x => x.Id == updated.Id);
        result.ShouldNotContain(x => x.Id == indicative.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Ppmp MakeApprovedPpmp(PpmpPhase phase, string description, decimal budget)
    {
        var ppmp = Ppmp.Create(
            $"PPMP-2027-ICT-{Guid.NewGuid():N}"[..18],
            fiscalYear: 2027,
            phase: phase,
            officeCode: "ICT",
            endUserUnit: "ICT Division",
            preparedById: Guid.NewGuid(),
            items:
            [
                new PpmpItemData(description, ProjectType.Goods, 1, "lot", "Shopping",
                    false, "01/2027", "03/2027", "04/2027", "General Fund", budget, null, null)
            ]);
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        return ppmp;
    }

    private static Ppmp MakeDraftPpmp(PpmpPhase phase, string description, decimal budget)
        => Ppmp.Create(
            $"PPMP-2027-ICT-{Guid.NewGuid():N}"[..18],
            fiscalYear: 2027,
            phase: phase,
            officeCode: "ICT",
            endUserUnit: "ICT Division",
            preparedById: Guid.NewGuid(),
            items:
            [
                new PpmpItemData(description, ProjectType.Goods, 1, "lot", "Shopping",
                    false, "01/2027", "03/2027", "04/2027", "General Fund", budget, null, null)
            ]);

    // SQLite in-memory: supports transactions + ExecuteUpdateAsync (unlike InMemory provider).
    // Returns both so callers can keep the connection alive as long as the DbContext.
    private static (ProcurementPlanningDbContext Db, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return (CreateDbContext(connection), connection);
    }

    private static ProcurementPlanningDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ProcurementPlanningDbContext>()
            .UseSqlite(connection)
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
        var db = new ProcurementPlanningDbContext(accessor, options, dbOptions, new StubHostEnvironment());
        db.Database.EnsureCreated();
        return db;
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
        public string ApplicationName { get; set; } = nameof(ConsolidatePpmpsHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

