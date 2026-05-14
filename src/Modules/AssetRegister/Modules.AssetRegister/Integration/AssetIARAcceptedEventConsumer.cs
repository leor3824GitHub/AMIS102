using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Integration;

/// <summary>
/// Materializes one <see cref="AssetRegistry"/> row per accepted physical unit
/// (per cardinality rule §B — a quantity of N produces N rows, each with its own
/// PropertyNo). PropertyNo is operator-assigned at IAR acceptance time and arrives
/// on the event line; lines without a number are skipped with a warning so the
/// operator can correct the IAR and re-fire.
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

        var catalogs = await db.PropertyItemCatalogs.ToListAsync(ct).ConfigureAwait(false);
        if (catalogs.Count == 0)
        {
            logger.LogWarning(
                "[{Tenant}] AssetRegister received IAR {IARId} but no PropertyItemCatalog entries exist; skipping materialization.",
                tenantId, @event.IARId);
            return;
        }

        var materialized = 0;
        var skipped = 0;

        foreach (var line in @event.AcceptedItems)
        {
            if (string.IsNullOrWhiteSpace(line.StockPropertyNo))
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR line '{Description}': StockPropertyNo not assigned. Operator must set the PropertyNo on the IAR line before acceptance.",
                    tenantId, line.Description);
                skipped++;
                continue;
            }

            var catalog = ResolveCatalog(catalogs, line);
            if (catalog is null)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR line '{Description}' (PropertyClassHint='{Hint}'): no matching PropertyItemCatalog.",
                    tenantId, line.Description, line.PropertyClassHint);
                skipped++;
                continue;
            }

            var (assetType, category) = ClassifyFromCatalog(catalog, line.UnitCost);
            var quantity = (int)Math.Floor(line.Quantity);
            if (quantity <= 0)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping IAR line '{Description}': non-positive quantity {Qty}.",
                    tenantId, line.Description, line.Quantity);
                skipped++;
                continue;
            }
            if (quantity != 1)
            {
                logger.LogWarning(
                    "[{Tenant}] IAR line '{Description}' has quantity {Qty} but only one StockPropertyNo. Per NFA policy, tracked items must use one IAR line per unit. Materializing 1 row.",
                    tenantId, line.Description, quantity);
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

    private static PropertyItemCatalog? ResolveCatalog(
        IReadOnlyList<PropertyItemCatalog> catalogs, AssetIARAcceptedEventItem line)
    {
        if (!string.IsNullOrWhiteSpace(line.PropertyClassHint))
        {
            var byClass = catalogs.FirstOrDefault(c =>
                c.IsActive &&
                string.Equals(c.DefaultPropertyClass, line.PropertyClassHint, StringComparison.OrdinalIgnoreCase));
            if (byClass is not null) return byClass;
        }

        // Fallback: match by description prefix.
        return catalogs.FirstOrDefault(c =>
            c.IsActive &&
            (line.Description.Contains(c.Description, StringComparison.OrdinalIgnoreCase)
             || c.Description.Contains(line.Description, StringComparison.OrdinalIgnoreCase)));
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

