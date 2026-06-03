using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
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
    // Used to classify materialized assets when no active CapitalizationThreshold master data row exists
    // (PPE vs SE at Php 50,000 §4.2; high- vs low-value SE at Php 5,000 §4.8 — COA Circular 2022-004).
    private static readonly CapitalizationThresholdDto FallbackThreshold = new(
        Guid.Empty, "COA Circular 2022-004 (default)", string.Empty,
        CapitalizationAmount: 50_000m, SemiExpendableLowValueThreshold: 5_000m,
        EffectivityDate: default, IsActive: true);

    public async Task HandleAsync(AssetIARAcceptedEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var tenantId = @event.TenantId ?? db.TenantInfo?.Identifier ?? string.Empty;

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
            ?? FallbackThreshold;

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

            var (assetType, category) = ClassifyFromCatalog(catalog, line.UnitCost, threshold);
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
                fundCluster: "01",
                acquisitionDate: DateOnly.FromDateTime(@event.OccurredOnUtc),
                unitCost: line.UnitCost,
                sourceIARId: @event.IARId,
                sourcePurchaseOrderId: @event.PurchaseOrderId);

            db.AssetRegistries.Add(asset);
            materialized++;
        }

        if (materialized > 0)
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "[{Tenant}] AssetRegister processed IAR {IARId}: materialized={Materialized} alreadyPresent={AlreadyPresent} skipped={Skipped} of {LineCount} lines.",
            tenantId, @event.IARId, materialized, alreadyPresent, skipped, @event.AcceptedItems.Count);
    }

    private static (AssetType, AssetCategory) ClassifyFromCatalog(
        PropertyItemCatalog catalog, decimal unitCost, CapitalizationThresholdDto threshold)
    {
        // A catalog item tagged PPE in its PropertyClass is booked as PPE even below the cutoff — COA wins.
        var isPpeClass = catalog.DefaultPropertyClass.Contains("PPE", StringComparison.OrdinalIgnoreCase);
        if (isPpeClass || threshold.IsCapitalAsset(unitCost))
            return (AssetType.PPE, AssetCategory.PPE);

        return threshold.IsHighValueSemiExpendable(unitCost)
            ? (AssetType.SE, AssetCategory.HighValuedSemi)
            : (AssetType.SE, AssetCategory.LowValuedSemi);
    }
}
