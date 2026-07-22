using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReport;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// The sending half of the handshake: posting a PPEIR with a destination agency must write the outbound
/// transfer offer in the SAME SaveChanges as the report, so the sender's books and the offer can never
/// disagree — and must refuse the combinations that would forge a document or leak to a bad tenant.
/// </summary>
public sealed class CreateIssuanceReportTransferOfferTests
{
    private const string AgencyA = "agency-a";
    private const string AgencyB = "agency-b";

    [Fact]
    public async Task Post_WithDestination_WritesTheOutboundOffer_AtomicallyWithTheReport()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var result = await NewHandler(host, db).Handle(
                NewCommand(assetId, IssuanceNature.TransferRO, AgencyB), CancellationToken.None);

            result.ReportNo.ShouldBe("PPEIR-2026-01-0001");
        });

        var offer = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers
            .Include(o => o.Lines).AsNoTracking().SingleOrDefaultAsync());

        offer.ShouldNotBeNull();
        offer.Direction.ShouldBe(TransferOfferDirection.Outbound);
        offer.Status.ShouldBe(TransferOfferStatus.Sent);
        offer.ToTenantId.ShouldBe(AgencyB);
        offer.ToAgencyName.ShouldBe("Agency AGENCY-B");
        offer.SourceIssuanceReportNo.ShouldBe("PPEIR-2026-01-0001");
        offer.OfferProjectedUtc.ShouldBeNull();   // queued for the projector, not yet delivered

        // Book values are snapshotted from the ASSET (system of record), not from the report line —
        // the line's AccumulatedDepreciation is filled later by Accounting and is still null here.
        var line = offer.Lines.Single();
        line.SourcePropertyNo.ShouldBe("2026-NFA-00B-07-DSK-001");
        line.UnitCost.ShouldBe(60_000m);
        line.AccumulatedDepreciation.ShouldBe(45_600m);
        line.DepreciationCurrentThrough.ShouldBe(new DateOnly(2026, 1, 1));
        line.NetBookValue.ShouldBe(14_400m);
        line.OriginalAcquisitionDate.ShouldBe(new DateOnly(2022, 1, 15));
    }

    [Fact]
    public async Task Post_WithoutDestination_WritesNoOffer()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
            await NewHandler(host, db).Handle(
                NewCommand(assetId, IssuanceNature.TransferRO, destinationTenantId: null), CancellationToken.None));

        // The legacy unlinked paper handshake still works, unchanged.
        (await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task Post_WithDestination_OnANonTransferNature_IsRejected()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var ex = await Should.ThrowAsync<CustomException>(() =>
                NewHandler(host, db).Handle(
                    NewCommand(assetId, IssuanceNature.Sale, AgencyB), CancellationToken.None).AsTask());

            ex.Message.ShouldContain("transfer issuance");
        });
    }

    [Fact]
    public async Task Post_WithAnUnknownDestination_IsRejected()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            // A tenant id from the client is never trusted — it must resolve in the registry and be active.
            var ex = await Should.ThrowAsync<CustomException>(() =>
                NewHandler(host, db).Handle(
                    NewCommand(assetId, IssuanceNature.TransferRO, "agency-ghost"), CancellationToken.None).AsTask());

            ex.Message.ShouldContain("unknown or deactivated");
        });
    }

    [Fact]
    public async Task Post_TransferringToSelf_IsRejected()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var ex = await Should.ThrowAsync<CustomException>(() =>
                NewHandler(host, db).Handle(
                    NewCommand(assetId, IssuanceNature.TransferRO, AgencyA), CancellationToken.None).AsTask());

            ex.Message.ShouldContain("itself");
        });
    }

    /// <summary>Scope for this pass is PPE only; SE reuses the same plumbing but is not wired yet.</summary>
    [Fact]
    public async Task Post_LinkedTransfer_OnASmir_IsRejected()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailableSeAsync(host);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var cmd = NewCommand(assetId, IssuanceNature.TransferRO, AgencyB) with
            {
                ReportType = IssuanceReportType.SMIR
            };

            var ex = await Should.ThrowAsync<CustomException>(() =>
                NewHandler(host, db).Handle(cmd, CancellationToken.None).AsTask());

            ex.Message.ShouldContain("PPE (PPEIR");
        });
    }

    // ── Destination derived from the recipient ──────────────────────────────────────────────

    [Fact]
    public async Task Post_WhenTheRecipientBelongsToADifferentAgencyThanTheDestination_IsRejected()
    {
        const string AgencyC = "agency-c";
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB, AgencyC);
        var assetId = await SeedAvailablePpeAsync(host);

        // The recipient works at an office that agency C claims, but the form names agency B as the
        // destination. Honouring that would put the assets on C's colleague's form and B's books.
        var officeId = Guid.NewGuid();
        host.TenantService.FindByOfficeIdAsync(officeId, Arg.Any<CancellationToken>())
            .Returns(host.Tenant(AgencyC));

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var cmd = NewCommand(assetId, IssuanceNature.TransferRO, AgencyB) with
            {
                IssuedTo = new EmployeeRefDto(Guid.NewGuid(), "Receiving Officer", "Custodian")
            };

            var ex = await Should.ThrowAsync<CustomException>(() =>
                NewHandler(host, db, EmployeeAt(officeId)).Handle(cmd, CancellationToken.None).AsTask());

            ex.Message.ShouldContain("Agency AGENCY-C");
        });

        (await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task Post_WhenTheRecipientBelongsToTheDestinationAgency_WritesTheOffer()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        var officeId = Guid.NewGuid();
        host.TenantService.FindByOfficeIdAsync(officeId, Arg.Any<CancellationToken>())
            .Returns(host.Tenant(AgencyB));

        await host.AsTenantAsync(AgencyA, async db =>
        {
            var cmd = NewCommand(assetId, IssuanceNature.TransferRO, AgencyB) with
            {
                IssuedTo = new EmployeeRefDto(Guid.NewGuid(), "Receiving Officer", "Custodian")
            };

            await NewHandler(host, db, EmployeeAt(officeId)).Handle(cmd, CancellationToken.None);
        });

        var offer = await host.AsTenantAsync(AgencyA, db =>
            db.AssetTransferOffers.AsNoTracking().SingleOrDefaultAsync());

        offer.ShouldNotBeNull();
        offer.ToTenantId.ShouldBe(AgencyB);
    }

    [Fact]
    public async Task Post_WithAHandTypedRecipient_SkipsTheAgencyCheck()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        // Guid.Empty means the name was typed, not picked — there is no employee row and therefore no
        // agency to reconcile against, so the transfer must still go through.
        await host.AsTenantAsync(AgencyA, async db =>
            await NewHandler(host, db).Handle(
                NewCommand(assetId, IssuanceNature.TransferRO, AgencyB), CancellationToken.None));

        (await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(1);
    }

    [Fact]
    public async Task Post_WithDestination_WithoutTheOfferPermission_IsRejected()
    {
        using var host = new MultiTenantTestHost(null, AgencyA, AgencyB);
        var assetId = await SeedAvailablePpeAsync(host);

        // The endpoint only demands Issuance.Create, so raising an offer has to be checked in the handler
        // — otherwise issuance rights alone would be enough to push assets onto another agency's books.
        await host.AsTenantAsync(AgencyA, async db =>
            await Should.ThrowAsync<ForbiddenException>(() =>
                NewHandler(host, db, recipient: null, mayOffer: false)
                    .Handle(NewCommand(assetId, IssuanceNature.TransferRO, AgencyB), CancellationToken.None)
                    .AsTask()));

        (await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(0);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A recipient employee assigned to <paramref name="officeId"/> — the office that decides their agency.</summary>
    private static EmployeeReferenceDto EmployeeAt(Guid officeId) =>
        new(
            Id: Guid.NewGuid(),
            EmployeeNumber: "EMP-001",
            IdentityUserId: null,
            FirstName: "Receiving",
            LastName: "Officer",
            WorkEmail: null,
            OfficeId: officeId,
            OfficeCode: "OFC",
            OfficeName: "Receiving Office",
            OfficeAddress: null,
            DepartmentId: Guid.NewGuid(),
            DepartmentCode: "DEP",
            DepartmentName: "Supply",
            PositionId: Guid.NewGuid(),
            PositionCode: "POS",
            PositionName: "Custodian",
            IsActive: true);

    private static CreateIssuanceReportCommandHandler NewHandler(
        MultiTenantTestHost host,
        AssetRegisterDbContext db,
        EmployeeReferenceDto? recipient = null,
        bool mayOffer = true)
    {
        var numbers = Substitute.For<IIssuanceReportNumberGenerator>();
        numbers.NextAsync(Arg.Any<IssuanceReportType>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns("PPEIR-2026-01-0001");

        var freezeGuard = Substitute.For<ICountFreezeGuard>();
        freezeGuard.EnsureMovementAllowedAsync(Arg.Any<IReadOnlyCollection<AssetRegistry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetOrganizationProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(new OrganizationProfileDto(
                Id: Guid.NewGuid(), Name: "Agency AGENCY-A", ShortName: null, Address: null,
                LogoUrl: null, AnnexECode: null,
                ApprovingOfficialName: "Regional Manager", ApprovingOfficialDesignation: "RM II"));

        var projector = new AssetTransferProjector(
            host.ScopeFactory, host.TenantService, NullLogger<AssetTransferProjector>.Instance);

        if (recipient is not null)
        {
            mediator.Send(Arg.Any<GetEmployeeReferenceByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(recipient);
        }

        var resolver = new TransferDestinationResolver(mediator, host.TenantService);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(Guid.NewGuid());

        // Most of these tests exercise the transfer rules, not authorization — the offer permission is
        // granted by default so the guard never masks the behaviour under test.
        var permissions = Substitute.For<IUserPermissionService>();
        permissions.HasPermissionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mayOffer);

        return new CreateIssuanceReportCommandHandler(
            db, numbers, freezeGuard, projector, resolver, currentUser, permissions, mediator);
    }

    private static CreateIssuanceReportCommand NewCommand(
        Guid assetId, IssuanceNature nature, string? destinationTenantId) =>
        new(
            ReportType: IssuanceReportType.PPEIR,
            Date: new DateOnly(2026, 1, 20),
            FundCluster: "01",
            Nature: nature,
            IssuedBy: new EmployeeRefDto(Guid.NewGuid(), "Supply Officer", "Officer"),
            IssuedTo: new EmployeeRefDto(Guid.Empty, "Receiving Officer", "Custodian"),
            IssuedToOfficeAddress: "Agency B, Region V",
            Remarks: null,
            AssetRegistryIds: [assetId],
            DestinationTenantId: destinationTenantId);

    /// <summary>A 4-year-old PPE asset that is part-way through its depreciation schedule.</summary>
    private static async Task<Guid> SeedAvailablePpeAsync(MultiTenantTestHost host)
    {
        var catalog = PropertyItemCatalog.Create(
            AgencyA, "DESK-001", "Office Desk", "07-PPE", "DSK", "pc", "10405030", 5);

        var asset = AssetRegistry.Register(
            AgencyA, catalog, AssetType.PPE, AssetCategory.PPE,
            PropertyNumber.Create("2026-NFA-00B-07-DSK-001"), "Office Desk",
            "SN-1", "Acme", "D100", "01", new DateOnly(2022, 1, 15), 60_000m, null, null,
            accumulatedDepreciation: 45_600m, depreciationCurrentThrough: new DateOnly(2026, 1, 1));

        await host.AsTenantAsync(AgencyA, async db =>
        {
            db.PropertyItemCatalogs.Add(catalog);
            db.AssetRegistries.Add(asset);
            await db.SaveChangesAsync();
        });

        return asset.Id;
    }

    private static async Task<Guid> SeedAvailableSeAsync(MultiTenantTestHost host)
    {
        var catalog = PropertyItemCatalog.Create(
            AgencyA, "CHAIR-001", "Monobloc Chair", "SE", "CHR", "pc", "10405020", 5);

        var asset = AssetRegistry.Register(
            AgencyA, catalog, AssetType.SE, AssetCategory.LowValuedSemi,
            PropertyNumber.Create("2026-NFA-00B-SE-CHR-001"), "Monobloc Chair",
            null, null, null, "01", new DateOnly(2026, 1, 15), 4_000m, null, null);

        await host.AsTenantAsync(AgencyA, async db =>
        {
            db.PropertyItemCatalogs.Add(catalog);
            db.AssetRegistries.Add(asset);
            await db.SaveChangesAsync();
        });

        return asset.Id;
    }
}
