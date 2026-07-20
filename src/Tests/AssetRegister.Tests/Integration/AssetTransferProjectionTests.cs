using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Transfers;
using AMIS.Modules.AssetRegister.Provisioning;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// End-to-end cover for the inter-agency transfer handshake across a real tenant-scope switch.
/// <para>
/// There is no cross-tenant row here: a transfer is two ordinary tenant-scoped rows joined by a correlation
/// id, and <see cref="AssetTransferProjector"/> is the only code allowed to cross the boundary. These tests
/// hold all tenants in ONE store behind Finbuckle's query filter, so an isolation assertion actually proves
/// the filter holds rather than proving the databases are different files.
/// </para>
/// </summary>
public sealed class AssetTransferProjectionTests
{
    private const string AgencyA = "agency-a";
    private const string AgencyB = "agency-b";
    private const string AgencyC = "agency-c";
    private const string CustodianLogin = "5c3a1f77-1111-4444-8888-bbbbbbbbbbbb";

    [Fact]
    public async Task Projector_DeliversTheOffer_IntoTheReceivingTenantOnly()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Receiver sees an inbound copy carrying the sender's book values.
        var inbound = await host.AsTenantAsync(AgencyB, db => db.AssetTransferOffers
            .Include(o => o.Lines).AsNoTracking()
            .SingleOrDefaultAsync(o => o.CorrelationId == correlationId));

        inbound.ShouldNotBeNull();
        inbound.Direction.ShouldBe(TransferOfferDirection.Inbound);
        inbound.Status.ShouldBe(TransferOfferStatus.Sent);
        inbound.FromAgencyName.ShouldBe("Agency AGENCY-A");
        inbound.TenantId.ShouldBe(AgencyB);
        inbound.Lines.Count.ShouldBe(1);

        var line = inbound.Lines.Single();
        line.SourcePropertyNo.ShouldBe("2026-NFA-00B-07-DSK-001");
        line.UnitCost.ShouldBe(60_000m);
        line.AccumulatedDepreciation.ShouldBe(45_600m);
        line.DepreciationCurrentThrough.ShouldBe(new DateOnly(2026, 1, 1));  // the cursor travels with the amount
        line.NetBookValue.ShouldBe(14_400m);

        // Sender's row is stamped delivered so it stops being retried.
        var outbound = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        outbound.OfferProjectedUtc.ShouldNotBeNull();
    }

    /// <summary>The isolation regression the plan calls for: an uninvolved agency must see nothing at all.</summary>
    [Fact]
    public async Task Projector_LeaksNothing_ToAnUninvolvedTenant()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        await SeedOutboundOfferAsync(host);

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var seenByC = await host.AsTenantAsync(AgencyC, db => db.AssetTransferOffers.AsNoTracking().CountAsync());
        seenByC.ShouldBe(0);

        var linesSeenByC = await host.AsTenantAsync(AgencyC, db => db.Set<AssetTransferOfferLine>().AsNoTracking().CountAsync());
        linesSeenByC.ShouldBe(0);

        // And each participant sees exactly its own single copy — not both.
        (await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(1);
        (await host.AsTenantAsync(AgencyB, db => db.AssetTransferOffers.AsNoTracking().CountAsync())).ShouldBe(1);
    }

    /// <summary>
    /// Delivery is at-least-once, so the job WILL re-run over the same offer. The unique
    /// (TenantId, CorrelationId) index is what makes the second pass a no-op instead of a duplicate.
    /// </summary>
    [Fact]
    public async Task Projector_IsIdempotent_WhenRunTwice()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Force a redelivery attempt by clearing the sender's stamp, as a crash between write and stamp would.
        await host.AsTenantAsync(AgencyA, async db =>
        {
            var outbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            typeof(AssetTransferOffer)
                .GetProperty(nameof(AssetTransferOffer.OfferProjectedUtc))!
                .SetValue(outbound, null);
            await db.SaveChangesAsync();
        });

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var inboundCount = await host.AsTenantAsync(AgencyB, db => db.AssetTransferOffers.AsNoTracking()
            .CountAsync(o => o.CorrelationId == correlationId));
        inboundCount.ShouldBe(1);   // not two
    }

    [Fact]
    public async Task Accept_CarriesTheResponseBack_ToTheSendingTenant()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);
        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Receiver accepts against its OWN receiving report number.
        var reportId = Guid.NewGuid();
        await host.AsTenantAsync(AgencyB, async db =>
        {
            var inbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            inbound.Accept(reportId, "PPERR-2026-0042");
            await db.SaveChangesAsync();
        });

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Sender's copy now reflects the decision — Accepted on BOTH sides.
        var outbound = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        outbound.Status.ShouldBe(TransferOfferStatus.Accepted);
        outbound.ReceivingReportNo.ShouldBe("PPERR-2026-0042");

        var inboundAfter = await host.AsTenantAsync(AgencyB, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        inboundAfter.Status.ShouldBe(TransferOfferStatus.Accepted);
        inboundAfter.ResponseProjectedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Reject_FlipsTheSendersCopy_AndCreatesNoAssetInTheReceivingTenant()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);
        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        await host.AsTenantAsync(AgencyB, async db =>
        {
            var inbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            inbound.Reject("Serial numbers do not match the shipment.");
            await db.SaveChangesAsync();
        });

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var outbound = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        outbound.Status.ShouldBe(TransferOfferStatus.Rejected);
        outbound.RejectedReason.ShouldBe("Serial numbers do not match the shipment.");

        // Rejection books nothing: accepting is what creates assets, via the receiver's own PPERR.
        var assetsInB = await host.AsTenantAsync(AgencyB, db => db.AssetRegistries.AsNoTracking().CountAsync());
        assetsInB.ShouldBe(0);
    }

    [Fact]
    public async Task ResponseCarryBack_IsIdempotent_WhenRunTwice()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);
        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        await host.AsTenantAsync(AgencyB, async db =>
        {
            var inbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            inbound.Accept(Guid.NewGuid(), "PPERR-2026-0042");
            await db.SaveChangesAsync();
        });

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Replay the carry-back as a crash-before-stamp would.
        await host.AsTenantAsync(AgencyB, async db =>
        {
            var inbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            typeof(AssetTransferOffer)
                .GetProperty(nameof(AssetTransferOffer.ResponseProjectedUtc))!
                .SetValue(inbound, null);
            await db.SaveChangesAsync();
        });

        // ApplyResponse no-ops on an unchanged status, so this must not throw.
        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var outbound = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        outbound.Status.ShouldBe(TransferOfferStatus.Accepted);
    }

    /// <summary>
    /// <c>Notification</c> is <c>.IsMultiTenant()</c>, so stamping TenantId on the event is not enough — the
    /// write has to happen under the recipient's ambient context. Asserting the event's tenant is the closest
    /// observable proxy for "the projector notified inside the receiving scope, not the sending one".
    /// </summary>
    [Fact]
    public async Task Projector_NotifiesTheRecipientTenant_NotTheSender()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);
        var correlationId = await SeedOutboundOfferAsync(host);

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var arrival = notifications.ShouldHaveSingleItem();
        arrival.Type.ShouldBe(NotificationType.TransferOfferReceived);
        arrival.TenantId.ShouldBe(AgencyB);           // delivered to the receiver, not agency-a
        arrival.RecipientUserId.ShouldBe(CustodianLogin);
        arrival.CorrelationId.ShouldBe($"transfer-offer-received:{correlationId}");

        // ...and the answer goes back the other way.
        notifications.Clear();
        await host.AsTenantAsync(AgencyB, async db =>
        {
            var inbound = await db.AssetTransferOffers.SingleAsync(o => o.CorrelationId == correlationId);
            inbound.Accept(Guid.NewGuid(), "PPERR-2026-0042");
            await db.SaveChangesAsync();
        });
        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        var answer = notifications.ShouldHaveSingleItem();
        answer.Type.ShouldBe(NotificationType.TransferOfferAnswered);
        answer.TenantId.ShouldBe(AgencyA);            // back to the sender
    }

    [Fact]
    public async Task Projector_SkipsDelivery_ToAnUnknownOrDeactivatedTenant()
    {
        var notifications = new List<NotificationRequestedIntegrationEvent>();
        using var host = NewHost(notifications);

        // Destination is not in the registry at all.
        var correlationId = Guid.NewGuid();
        await host.AsTenantAsync(AgencyA, async db =>
        {
            db.AssetTransferOffers.Add(NewOffer(correlationId, toTenantId: "agency-ghost"));
            await db.SaveChangesAsync();
        });

        await NewJob(host, notifications).RunAsync(CancellationToken.None);

        // Left unstamped so a later run retries once the tenant exists — never silently dropped.
        var outbound = await host.AsTenantAsync(AgencyA, db => db.AssetTransferOffers.AsNoTracking()
            .SingleAsync(o => o.CorrelationId == correlationId));
        outbound.OfferProjectedUtc.ShouldBeNull();
        notifications.ShouldBeEmpty();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static MultiTenantTestHost NewHost(List<NotificationRequestedIntegrationEvent> notifications) =>
        new(services =>
            {
                services.AddSingleton(NotifyingMediator());
                services.AddSingleton(CapturingEventBus(notifications));
            },
            AgencyA, AgencyB, AgencyC);

    private static AssetTransferProjector NewProjector(MultiTenantTestHost host) =>
        new(host.ScopeFactory, host.TenantService, NullLogger<AssetTransferProjector>.Instance);

    private static AssetTransferProjectionJob NewJob(
        MultiTenantTestHost host, List<NotificationRequestedIntegrationEvent> notifications)
    {
        _ = notifications;
        return new AssetTransferProjectionJob(
            host.ScopeFactory, host.TenantService, NewProjector(host),
            NullLogger<AssetTransferProjectionJob>.Instance);
    }

    /// <summary>
    /// Seeds the outbound row exactly as <c>CreateIssuanceReportCommandHandler</c> writes it alongside the
    /// PPEIR. The handler's own behaviour is covered separately; these tests are about the boundary crossing.
    /// </summary>
    private static async Task<Guid> SeedOutboundOfferAsync(MultiTenantTestHost host)
    {
        var correlationId = Guid.NewGuid();
        await host.AsTenantAsync(AgencyA, async db =>
        {
            db.AssetTransferOffers.Add(NewOffer(correlationId, AgencyB));
            await db.SaveChangesAsync();
        });
        return correlationId;
    }

    private static AssetTransferOffer NewOffer(Guid correlationId, string toTenantId)
    {
        var offer = AssetTransferOffer.CreateOutbound(
            tenantId: AgencyA, correlationId: correlationId, fromAgencyName: "Agency AGENCY-A",
            toTenantId: toTenantId, toAgencyName: "Agency AGENCY-B",
            sourceIssuanceReportId: Guid.NewGuid(), sourceIssuanceReportNo: "PPEIR-2026-0007",
            issuanceReportType: IssuanceReportType.PPEIR);

        offer.AddLine(
            "2026-NFA-00B-07-DSK-001", "Office Desk", "SN-1", "Acme", "D100",
            unitCost: 60_000m, originalAcquisitionDate: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m, depreciationCurrentThrough: new DateOnly(2026, 1, 1),
            netBookValue: 14_400m, catalogUacsCode: "10405030");

        return offer;
    }

    private static IEventBus CapturingEventBus(List<NotificationRequestedIntegrationEvent> sink)
    {
        var eventBus = Substitute.For<IEventBus>();
        eventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                if (ci.Arg<IIntegrationEvent>() is NotificationRequestedIntegrationEvent n)
                    sink.Add(n);
            });
        return eventBus;
    }

    /// <summary>An org profile with a property custodian who has a login — so a notification has a target.</summary>
    private static IMediator NotifyingMediator()
    {
        var custodianId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<GetOrganizationProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(new OrganizationProfileDto(
                Id: Guid.NewGuid(), Name: "Test Agency", ShortName: null, Address: null,
                LogoUrl: null, AnnexECode: null, PropertyCustodianId: custodianId,
                PropertyCustodianName: "Property Custodian", PropertyCustodianDesignation: "Custodian"));

        mediator.Send(Arg.Any<GetEmployeeReferencesByIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, EmployeeReferenceDto>)new Dictionary<Guid, EmployeeReferenceDto>
            {
                [custodianId] = new(
                    custodianId, "EMP-CUST", CustodianLogin, "Property", "Custodian", null,
                    Guid.NewGuid(), "OFF", "Office", "Office Address", Guid.NewGuid(), "DEP", "Dept",
                    Guid.NewGuid(), "POS", "Custodian", true)
            });

        return mediator;
    }
}
