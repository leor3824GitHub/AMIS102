using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Transfers;
using AMIS.Modules.Multitenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Provisioning;

/// <summary>
/// Drains inter-agency transfer offers across the tenant boundary. The offer row IS the outbox: the sender
/// writes its PPEIR and the outbound offer in one atomic SaveChanges, and this job later delivers the
/// inbound copy to the receiving tenant and carries responses back.
/// <para>
/// Two queues, both scanned per tenant inside that tenant's own DI scope:
/// <list type="number">
/// <item>Outbound rows with <c>OfferProjectedUtc IS NULL</c> → project the inbound copy into ToTenantId.</item>
/// <item>Inbound rows answered but with <c>ResponseProjectedUtc IS NULL</c> → carry the response to FromTenantId.</item>
/// </list>
/// </para>
/// <para>
/// Safe to re-run: the unique (TenantId, CorrelationId) index makes a repeated projection a no-op, and
/// <c>ApplyResponse</c> is idempotent. A failure in one tenant is logged and does not abort the rest — the
/// same per-tenant isolation <c>DepreciationRecurringJob</c> uses.
/// </para>
/// </summary>
public sealed class AssetTransferProjectionJob(
    IServiceScopeFactory scopeFactory,
    ITenantService tenantService,
    AssetTransferProjector projector,
    ILogger<AssetTransferProjectionJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await tenantService.GetAllTenantInfosAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive)
                continue;

            try
            {
                await DeliverOffersAsync(tenant, cancellationToken).ConfigureAwait(false);
                await DeliverResponsesAsync(tenant, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Isolate per-tenant failures so one bad tenant doesn't strand every other agency's transfers.
                logger.LogError(ex,
                    "AssetTransferProjectionJob: failed to drain transfer offers for tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private async Task DeliverOffersAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        SetTenant(scope.ServiceProvider, tenant);
        var db = scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>();

        var pending = await db.AssetTransferOffers
            .Include(o => o.Lines)
            .Where(o => o.Direction == TransferOfferDirection.Outbound && o.OfferProjectedUtc == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (pending.Count == 0)
            return;

        foreach (var offer in pending)
        {
            var payload = new TransferOfferPayload(
                offer.CorrelationId, offer.FromTenantId, offer.FromAgencyName, offer.ToTenantId,
                offer.ToAgencyName, offer.SourceIssuanceReportId, offer.SourceIssuanceReportNo,
                offer.IssuanceReportType, offer.CreatedOnUtc,
                [.. offer.Lines.OrderBy(l => l.ItemNo).Select(l => new TransferOfferLinePayload(
                    l.SourcePropertyNo, l.Description, l.SerialNo, l.Brand, l.Model, l.UnitCost,
                    l.OriginalAcquisitionDate, l.AccumulatedDepreciation, l.DepreciationCurrentThrough,
                    l.NetBookValue, l.CatalogUacsCode))]);

            var delivered = await projector.ProjectOfferAsync(payload, cancellationToken).ConfigureAwait(false);
            if (delivered)
                offer.MarkOfferProjected();
        }

        // Stamp only what actually landed; anything still unstamped is retried on the next pass.
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeliverResponsesAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        SetTenant(scope.ServiceProvider, tenant);
        var db = scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>();

        var answered = await db.AssetTransferOffers
            .Where(o => o.Direction == TransferOfferDirection.Inbound
                     && o.Status != TransferOfferStatus.Sent
                     && o.ResponseProjectedUtc == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (answered.Count == 0)
            return;

        foreach (var offer in answered)
        {
            var carried = await projector.ProjectResponseAsync(
                offer.CorrelationId, offer.FromTenantId, offer.Status,
                offer.ReceivingReportId, offer.ReceivingReportNo, offer.RejectedReason,
                offer.RespondedUtc ?? DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);

            if (carried)
                offer.MarkResponseProjected();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void SetTenant(IServiceProvider services, AppTenantInfo tenant) =>
        services.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
}
