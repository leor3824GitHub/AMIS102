using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Integration;

/// <summary>
/// Materializes one <see cref="AssetRegistry"/> row per accepted physical unit. As of Phase 3, every accepted
/// IAR line MUST carry an explicit <c>CatalogItemId</c> — fuzzy matching (PropertyClassHint / description /
/// token overlap) has been removed. Lines missing either the catalog id or a Property No are skipped with a
/// warning so the operator can correct the source IAR and re-fire if needed.
/// </summary>
internal sealed class AssetIARAcceptedEventConsumer(
    AssetRegisterDbContext db,
    IMediator mediator,
    ILogger<AssetIARAcceptedEventConsumer> logger) : IIntegrationEventHandler<AssetIARAcceptedEvent>
{
    // Fund cluster used only when the source PO carried none (legacy/incomplete data). "01" is the
    // Regular Agency Fund — the overwhelming default — but a real cluster from the event always wins
    // so multi-fund agencies book each asset against the fund its PO was charged to.
    private const string DefaultFundCluster = "01";

    public async Task HandleAsync(AssetIARAcceptedEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var tenantId = @event.TenantId ?? db.TenantInfo?.Identifier ?? string.Empty;
        var fundCluster = string.IsNullOrWhiteSpace(@event.FundCluster) ? DefaultFundCluster : @event.FundCluster;

        var ids = @event.AcceptedItems
            .Where(li => li.CatalogItemId is not null && li.CatalogItemId != Guid.Empty)
            .Select(li => li.CatalogItemId!.Value)
            .Distinct()
            .ToList();

        var catalogsById = ids.Count == 0
            ? new Dictionary<Guid, PropertyItemCatalog>()
            : await db.PropertyItemCatalogs.Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct).ConfigureAwait(false);

        var threshold = await mediator.Send(new GetActiveCapitalizationThresholdQuery(), ct).ConfigureAwait(false)
            ?? AssetClassificationPolicy.FallbackThreshold;

        var materialized = 0;
        var skipped = 0;
        var alreadyPresent = 0;

        foreach (var line in @event.AcceptedItems)
        {
            if (string.IsNullOrWhiteSpace(line.StockPropertyNo))
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR {IARId} line '{Description}': StockPropertyNo not assigned.",
                    tenantId, @event.IARId, line.Description);
                skipped++;
                continue;
            }

            if (line.CatalogItemId is null || line.CatalogItemId == Guid.Empty)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR {IARId} line '{Description}': no CatalogItemId. " +
                    "Source PR must reference a catalog item before acceptance.",
                    tenantId, @event.IARId, line.Description);
                skipped++;
                continue;
            }

            if (!catalogsById.TryGetValue(line.CatalogItemId.Value, out var catalog))
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR {IARId} line '{Description}': catalog {CatalogItemId} not found.",
                    tenantId, @event.IARId, line.Description, line.CatalogItemId);
                skipped++;
                continue;
            }

            if (catalog.Status == CatalogItemStatus.Draft || string.IsNullOrWhiteSpace(catalog.UacsObjectCode))
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR {IARId} line '{Description}': catalog '{Code}' is still Draft (UACS missing). " +
                    "Accountant must certify Funds Available on the source PR first.",
                    tenantId, @event.IARId, line.Description, catalog.Code);
                skipped++;
                continue;
            }

            var (assetType, category) = AssetClassificationPolicy.Classify(catalog, line.UnitCost, threshold);
            var quantity = (int)Math.Floor(line.Quantity);
            if (quantity <= 0)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR {IARId} line '{Description}': non-positive quantity {Qty}.",
                    tenantId, @event.IARId, line.Description, line.Quantity);
                skipped++;
                continue;
            }
            if (quantity != 1)
            {
                logger.LogWarning(
                    "[{Tenant}] IAR line '{Description}' has quantity {Qty} but only one StockPropertyNo. " +
                    "Per NFA policy, tracked items must use one IAR line per unit. Materializing 1 row.",
                    tenantId, line.Description, quantity);
            }

            var propertyNo = PropertyNumber.Create(line.StockPropertyNo);

            // Idempotency (line-level): a unit whose Property No is already registered was materialized by an
            // earlier delivery. Skip it — but keep processing sibling lines, so an operator can correct a
            // previously-skipped line and re-fire the IAR without tripping the PropertyNo unique constraint.
            // A line-level guard is used instead of an IAR-level inbox marker precisely to preserve that re-fire
            // workflow (an IAR may be only partially materialized on the first pass).
            var alreadyMaterialized = await db.AssetRegistries
                .AnyAsync(a => a.PropertyNo == propertyNo, ct)
                .ConfigureAwait(false);
            if (alreadyMaterialized)
            {
                logger.LogInformation(
                    "[{Tenant}] IAR {IARId} line '{Description}' (Property No {PropertyNo}) already registered; " +
                    "skipping (idempotent redelivery / re-fire).",
                    tenantId, @event.IARId, line.Description, line.StockPropertyNo);
                alreadyPresent++;
                continue;
            }

            var asset = AssetRegistry.Register(
                tenantId,
                catalog,
                assetType,
                category,
                propertyNo,
                description: line.Description,
                serialNo: line.SerialNo,
                brand: line.Brand,
                model: line.Model,
                fundCluster: fundCluster,
                acquisitionDate: DateOnly.FromDateTime(@event.OccurredOnUtc),
                unitCost: line.UnitCost,
                sourceIARId: @event.IARId,
                sourcePurchaseOrderId: @event.PurchaseOrderId);

            db.AssetRegistries.Add(asset);
            materialized++;
        }

        if (materialized > 0)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                // The AnyAsync(PropertyNo) guard above and this Add are not one atomic operation, so a
                // concurrent redelivery of the same IAR can pass the check on two workers at once; the
                // PropertyNo unique index then rejects the loser at SaveChanges. That is the idempotent
                // outcome we want (the unit is already registered), so swallow it rather than surfacing a
                // 500 / poisoning the message — the winning worker has persisted the row.
                logger.LogInformation(
                    ex,
                    "[{Tenant}] AssetRegister IAR {IARId}: concurrent redelivery lost the PropertyNo race; " +
                    "treating as already registered (idempotent).",
                    tenantId, @event.IARId);
                return;
            }
        }

        logger.LogInformation(
            "[{Tenant}] AssetRegister processed IAR {IARId}: materialized={Materialized} alreadyPresent={AlreadyPresent} skipped={Skipped} of {LineCount} lines.",
            tenantId, @event.IARId, materialized, alreadyPresent, skipped, @event.AcceptedItems.Count);
    }
}
