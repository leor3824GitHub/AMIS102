using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Domain.Transfers;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>An offer line, read out of the sending tenant's row by the caller and handed to the projector.</summary>
public sealed record TransferOfferLinePayload(
    string SourcePropertyNo,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    decimal UnitCost,
    DateOnly OriginalAcquisitionDate,
    decimal AccumulatedDepreciation,
    DateOnly? DepreciationCurrentThrough,
    decimal NetBookValue,
    string? CatalogUacsCode);

/// <summary>
/// Everything the receiving agency is told about an incoming transfer — a deliberate, minimal disclosure
/// across the tenant boundary. Only the lines actually being transferred; never the sender's catalog,
/// custodians, asset ids, or unrelated assets.
/// </summary>
public sealed record TransferOfferPayload(
    Guid CorrelationId,
    string FromTenantId,
    string FromAgencyName,
    string ToTenantId,
    string ToAgencyName,
    Guid SourceIssuanceReportId,
    string SourceIssuanceReportNo,
    IssuanceReportType IssuanceReportType,
    DateTimeOffset OfferedOnUtc,
    IReadOnlyList<TransferOfferLinePayload> Lines);

/// <summary>
/// The single seam in this module that crosses a tenant boundary. Everything else runs under the ordinary
/// ambient tenant filters; keep it that way.
/// <para>
/// It never bypasses a query filter and never reads one tenant's data while scoped to another. The caller
/// reads the source row under its own ambient tenant and hands over a payload; the projector opens a fresh
/// DI scope, sets the ambient multi-tenant context to the destination tenant, and writes there — the pattern
/// <c>DepreciationRecurringJob</c> already proves. Two writes in two tenant scopes joined by a correlation
/// id; never one write, because Finbuckle rejects saving another tenant's rows through the current context
/// and tenants may live in physically separate databases.
/// </para>
/// <para>
/// Delivery is at-least-once, made safe by the unique (TenantId, CorrelationId) index: a replayed
/// projection collides on insert and is treated as already-delivered.
/// </para>
/// </summary>
public sealed class AssetTransferProjector(
    IServiceScopeFactory scopeFactory,
    ITenantService tenantService,
    ILogger<AssetTransferProjector> logger)
{
    /// <summary>
    /// Writes the inbound copy of an offer into the receiving tenant. Returns true when the inbound row
    /// exists afterwards — whether this call created it or an earlier pass already did — so the caller may
    /// stamp the sender's row as projected and stop retrying.
    /// </summary>
    public async Task<bool> ProjectOfferAsync(TransferOfferPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var tenant = await ResolveActiveTenantAsync(payload.ToTenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            return false;

        await using var scope = scopeFactory.CreateAsyncScope();
        SetTenant(scope.ServiceProvider, tenant);
        var db = scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>();

        var alreadyDelivered = await db.AssetTransferOffers
            .AnyAsync(o => o.CorrelationId == payload.CorrelationId, cancellationToken).ConfigureAwait(false);
        if (alreadyDelivered)
            return true;

        var inbound = AssetTransferOffer.CreateInbound(
            tenant.Identifier, payload.CorrelationId, payload.FromTenantId, payload.FromAgencyName,
            payload.ToAgencyName, payload.SourceIssuanceReportId, payload.SourceIssuanceReportNo,
            payload.IssuanceReportType, payload.OfferedOnUtc);

        foreach (var line in payload.Lines)
        {
            inbound.AddLine(
                line.SourcePropertyNo, line.Description, line.SerialNo, line.Brand, line.Model,
                line.UnitCost, line.OriginalAcquisitionDate, line.AccumulatedDepreciation,
                line.DepreciationCurrentThrough, line.NetBookValue, line.CatalogUacsCode);
        }

        db.AssetTransferOffers.Add(inbound);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            // A concurrent pass won the race and the unique (TenantId, CorrelationId) index rejected ours.
            // The offer IS delivered, so report success and let the sender's row be stamped.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(ex,
                    "AssetTransferProjector: offer {CorrelationId} was already projected into tenant {TenantId} by a concurrent run.",
                    payload.CorrelationId, payload.ToTenantId);
            }

            return true;
        }

        // Notify inside the SAME scope, whose ambient tenant is the recipient's. Notification is
        // .IsMultiTenant(), so stamping TenantId on the event is not enough — NotificationWriter has to run
        // under the recipient's context or the row lands in the wrong agency's inbox (or is rejected).
        await NotifyPropertyCustodianAsync(
            scope.ServiceProvider,
            tenant,
            NotificationType.TransferOfferReceived,
            "Incoming property transfer",
            $"{payload.FromAgencyName} has offered {payload.Lines.Count} item(s) for transfer via {payload.SourceIssuanceReportNo}. Review and accept or reject.",
            "/asset-register/transfers",
            $"transfer-offer-received:{payload.CorrelationId}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Carries a receiver's accept/reject decision back onto the sending tenant's outbound row. Idempotent:
    /// <see cref="AssetTransferOffer.ApplyResponse"/> no-ops when the status already matches, which is what
    /// makes an at-least-once carry-back safe.
    /// </summary>
    public async Task<bool> ProjectResponseAsync(
        Guid correlationId,
        string senderTenantId,
        TransferOfferStatus status,
        Guid? receivingReportId,
        string? receivingReportNo,
        string? rejectedReason,
        DateTimeOffset respondedUtc,
        CancellationToken cancellationToken)
    {
        var tenant = await ResolveActiveTenantAsync(senderTenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            return false;

        await using var scope = scopeFactory.CreateAsyncScope();
        SetTenant(scope.ServiceProvider, tenant);
        var db = scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>();

        var outbound = await db.AssetTransferOffers
            .FirstOrDefaultAsync(
                o => o.CorrelationId == correlationId && o.Direction == TransferOfferDirection.Outbound,
                cancellationToken)
            .ConfigureAwait(false);

        if (outbound is null)
        {
            logger.LogWarning(
                "AssetTransferProjector: no outbound offer for correlation {CorrelationId} in tenant {TenantId}; cannot carry the response back.",
                correlationId, senderTenantId);
            return false;
        }

        outbound.ApplyResponse(status, receivingReportId, receivingReportNo, rejectedReason, respondedUtc);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var outcome = status == TransferOfferStatus.Accepted
            ? $"accepted your transfer {outbound.SourceIssuanceReportNo} and booked it as {receivingReportNo}."
            : $"rejected your transfer {outbound.SourceIssuanceReportNo}. Reason: {rejectedReason}";

        await NotifyPropertyCustodianAsync(
            scope.ServiceProvider,
            tenant,
            NotificationType.TransferOfferAnswered,
            $"Transfer {status.ToString().ToLowerInvariant()}",
            $"{outbound.ToAgencyName} has {outcome}",
            "/asset-register/transfers",
            $"transfer-offer-answered:{correlationId}:{status}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Drops a notification in the recipient agency's property custodian's inbox.
    /// <para>
    /// <paramref name="services"/> MUST be a scope whose ambient tenant is already the recipient's — the
    /// notification row is tenant-scoped and is written under that ambient context, not from the TenantId on
    /// the event. Best-effort: a transfer is not failed because its notification could not be delivered, and
    /// the recurring job would otherwise retry the whole projection over a cosmetic error.
    /// </para>
    /// </summary>
    private async Task NotifyPropertyCustodianAsync(
        IServiceProvider services,
        AppTenantInfo tenant,
        NotificationType type,
        string title,
        string body,
        string link,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var mediator = services.GetRequiredService<IMediator>();

            var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
            if (org?.PropertyCustodianId is not { } custodianId || custodianId == Guid.Empty)
                return;

            var employees = await mediator
                .Send(new GetEmployeeReferencesByIdsQuery([custodianId]), cancellationToken).ConfigureAwait(false);

            if (!employees.TryGetValue(custodianId, out var employee)
                || string.IsNullOrWhiteSpace(employee.IdentityUserId))
            {
                return;
            }

            var eventBus = services.GetRequiredService<IEventBus>();
            await eventBus.PublishAsync(
                new NotificationRequestedIntegrationEvent(
                    RecipientUserId: employee.IdentityUserId,
                    Type: type,
                    Title: title,
                    Body: body,
                    Link: link,
                    Source: "AssetRegister",
                    MetadataJson: null,
                    TenantId: tenant.Identifier,
                    CorrelationId: correlationId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "AssetTransferProjector: could not notify tenant {TenantId} about '{Title}'. The transfer itself is unaffected.",
                tenant.Id, title);
        }
    }

    /// <summary>
    /// Resolves a tenant id to its registry entry, rejecting unknown and deactivated tenants. Never accept a
    /// destination tenant id from a client without passing it through here first.
    /// </summary>
    public async Task<AppTenantInfo?> ResolveActiveTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return null;

        var tenants = await tenantService.GetAllTenantInfosAsync(cancellationToken).ConfigureAwait(false);
        var tenant = tenants.FirstOrDefault(t =>
            string.Equals(t.Identifier, tenantId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.Id, tenantId, StringComparison.OrdinalIgnoreCase));

        if (tenant is null)
        {
            logger.LogWarning("AssetTransferProjector: tenant '{TenantId}' is not in the registry.", tenantId);
            return null;
        }

        if (!tenant.IsActive)
        {
            logger.LogWarning("AssetTransferProjector: tenant '{TenantId}' is deactivated.", tenantId);
            return null;
        }

        return tenant;
    }

    private static void SetTenant(IServiceProvider services, AppTenantInfo tenant) =>
        services.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
}
