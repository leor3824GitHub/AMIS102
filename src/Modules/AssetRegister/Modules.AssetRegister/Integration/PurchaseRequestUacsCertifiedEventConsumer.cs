using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Integration;

/// <summary>
/// Back-fills UACS Object Code on <c>PropertyItemCatalog</c> rows that were created inline from a PR
/// (status = Draft, UACS = null) once the Accountant certifies "Funds Available" on the PR. Promotes
/// the catalog row to <c>Ready</c> so downstream documents (PO/IAR/PPERR) can register assets against it.
///
/// Idempotent: rows already <c>Ready</c> or already carrying a UACS are left untouched.
/// </summary>
internal sealed class PurchaseRequestUacsCertifiedEventConsumer(
    AssetRegisterDbContext db,
    ILogger<PurchaseRequestUacsCertifiedEventConsumer> logger)
    : IIntegrationEventHandler<PurchaseRequestUacsCertifiedEvent>
{
    public async Task HandleAsync(PurchaseRequestUacsCertifiedEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var tenantId = @event.TenantId ?? db.TenantInfo?.Identifier ?? string.Empty;

        if (@event.Lines.Count == 0)
            return;

        var ids = @event.Lines.Select(l => l.CatalogItemId).Distinct().ToList();
        var catalogs = await db.PropertyItemCatalogs
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct).ConfigureAwait(false);
        var byId = catalogs.ToDictionary(c => c.Id);

        var promoted = 0;
        var skipped = 0;
        foreach (var line in @event.Lines)
        {
            if (!byId.TryGetValue(line.CatalogItemId, out var catalog))
            {
                logger.LogWarning(
                    "[{Tenant}] PR {PrNumber} item {ItemNo}: catalog {CatalogItemId} not found; skipping back-fill.",
                    tenantId, @event.PrNumber, line.ItemNo, line.CatalogItemId);
                skipped++;
                continue;
            }

            if (catalog.BackfillUacs(line.UacsObjectCode))
                promoted++;
            else
                skipped++;
        }

        if (promoted > 0)
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "[{Tenant}] PR {PrNumber} UACS certification: promoted {Promoted} catalog row(s) to Ready; skipped {Skipped} of {Total}.",
            tenantId, @event.PrNumber, promoted, skipped, @event.Lines.Count);
    }
}
