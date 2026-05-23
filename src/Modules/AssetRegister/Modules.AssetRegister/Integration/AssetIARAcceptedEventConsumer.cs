using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
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
    ILogger<AssetIARAcceptedEventConsumer> logger) : IIntegrationEventHandler<AssetIARAcceptedEvent>
{
    // High-valued threshold per COA 2022-004 §4.2 — Php 50,000.
    private const decimal HighValuedThreshold = 50_000m;

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

        var materialized = 0;
        var skipped = 0;

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

            var (assetType, category) = ClassifyFromCatalog(catalog, line.UnitCost);
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
                    line.Description, quantity);
            }

            var propertyNo = PropertyNumber.Create(line.StockPropertyNo);
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
            "[{Tenant}] AssetRegister processed IAR {IARId}: materialized={Materialized} skipped={Skipped} of {LineCount} lines.",
            tenantId, @event.IARId, materialized, skipped, @event.AcceptedItems.Count);
    }

    private static (AssetType, AssetCategory) ClassifyFromCatalog(PropertyItemCatalog catalog, decimal unitCost)
    {
        // PropertyClass naming convention: contains "PPE" → PPE; else SE.
        if (catalog.DefaultPropertyClass.Contains("PPE", StringComparison.OrdinalIgnoreCase))
            return (AssetType.PPE, AssetCategory.PPE);

        return unitCost >= HighValuedThreshold
            ? (AssetType.SE, AssetCategory.HighValuedSemi)
            : (AssetType.SE, AssetCategory.LowValuedSemi);
    }
}
