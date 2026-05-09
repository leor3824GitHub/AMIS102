using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetManagement.Provisioning;

/// <summary>
/// Hangfire recurring job that marks Active ICS records as Expired when their
/// <see cref="InventoryCustodianSlip.ExpiresOn"/> date has passed.
///
/// Runs daily. Safe to re-run: already-expired records are skipped.
///
/// COA Circular 2022-004 Section 4.11:
///   - High-valued ICS (SPHV): should be renewed before expiry; if not renewed, this job marks them Expired.
///   - Low-valued ICS (SPLV): expires automatically; property units remain Issued until an RLSDDSP or RRSP is filed.
/// </summary>
public sealed class ICSExpiryJob(AssetManagementDbContext dbContext, ILogger<ICSExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // IgnoreQueryFilters bypasses both the Finbuckle multi-tenant filter (which has
        // no tenant context in a Hangfire job) and the named soft-delete filter.
        // Soft-delete is re-applied manually. The expiry job intentionally spans all tenants.
        var expiredIcs = await dbContext.InventoryCustodianSlips
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                     && x.Status == ICSStatus.Active
                     && x.ExpiresOn.HasValue
                     && x.ExpiresOn.Value < today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredIcs.Count == 0)
        {
            return;
        }

        var expiredIcsIds = expiredIcs.Select(x => x.Id).ToList();
        var expiredIcsItems = await dbContext.ICSItems
            .IgnoreQueryFilters()
            .Where(x => expiredIcsIds.Contains(x.ICSId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryItemIds = expiredIcsItems
            .Select(x => x.TangibleInventoryItemId)
            .Distinct()
            .ToList();

        var inventoryItemsById = await dbContext.TangibleInventoryItems
            .IgnoreQueryFilters()
            .Where(x => inventoryItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await dbContext.AssetRegistry
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && inventoryItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        var itemsByIcsId = expiredIcsItems
            .GroupBy(x => x.ICSId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var ics in expiredIcs)
        {
            ics.MarkExpired();

            if (!itemsByIcsId.TryGetValue(ics.Id, out var icsItems))
            {
                continue;
            }

            foreach (var icsItem in icsItems)
            {
                if (!registryByInventoryItemId.TryGetValue(icsItem.TangibleInventoryItemId, out var registry))
                {
                    if (!inventoryItemsById.TryGetValue(icsItem.TangibleInventoryItemId, out var invItem))
                    {
                        continue;
                    }

                    // Backfill path: older records may predate AssetRegistry rollout.
                    // We intentionally snapshot from the current tangible inventory row so
                    // expiry history remains linked even when a registry was never created.
                    registry = AssetRegistry.Create(
                        tenantId: invItem.TenantId,
                        tangibleInventoryItemId: invItem.Id,
                        itemId: invItem.ItemId,
                        propertyNo: invItem.PropertyNo,
                        assetType: invItem.AssetType,
                        acquisitionDate: invItem.AcquisitionDate,
                        unitCost: invItem.UnitCost);

                    dbContext.AssetRegistry.Add(registry);
                    registryByInventoryItemId[invItem.Id] = registry;
                }

                var history = AssetAssignmentHistory.Create(
                    registry.TenantId,
                    registry.Id,
                    AssetAssignmentEventType.StatusChanged,
                    DateTimeOffset.UtcNow,
                    "ICS-EXPIRY",
                    ics.Id,
                    ics.ICSNo,
                    registry.CurrentCustodianId,
                    registry.CurrentCustodianId,
                    registry.CurrentLocationId,
                    "ICS automatically marked expired by scheduled job.");

                dbContext.AssetAssignmentHistory.Add(history);
                registry.LinkCurrentAssignment(history.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ICSExpiryJob: marked {Count} ICS record(s) as Expired (run date: {Today})",
                expiredIcs.Count, today);
        }
    }
}
