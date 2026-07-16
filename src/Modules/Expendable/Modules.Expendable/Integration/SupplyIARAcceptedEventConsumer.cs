using AMIS.Framework.Caching;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Eventing.Inbox;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Warehouse;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Expendable.Integration;

/// <summary>
/// Lands accepted <b>Supply</b> IAR lines into the warehouse <see cref="ProductInventory"/> ledger.
/// ProcurementAcquisition is the sole receiving/inspection authority; on a Supply IAR acceptance it
/// publishes <see cref="SupplyIARAcceptedEvent"/> (accepted, non-rejected lines only). Each line is matched
/// to an Expendable <c>Product</c> by <c>StockNo</c> — products are screened at PR time, so a match should
/// exist; lines that cannot be resolved are logged and skipped (mirrors AssetRegister's skip-and-warn).
/// All stock lands at the single <see cref="ExpendableModuleConstants.DefaultSupplyLocation"/>.
/// </summary>
internal sealed class SupplyIARAcceptedEventConsumer(
    ExpendableDbContext db,
    ICacheService cache,
    ILogger<SupplyIARAcceptedEventConsumer> logger) : IIntegrationEventHandler<SupplyIARAcceptedEvent>
{
    // Idempotency key for the inbox marker. Integration events are delivered at-least-once (outbox retry,
    // RabbitMQ redelivery), so a redelivery must not run twice — inventory is a running total and would
    // double-count. We key the marker on the stable IARId, not @event.Id (which is a get-only Guid.NewGuid()
    // regenerated on every deserialize, so it can't survive the distributed path).
    private const string HandlerName = "Expendable.SupplyIARAcceptedEventConsumer";

    public async Task HandleAsync(SupplyIARAcceptedEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var tenantId = @event.TenantId ?? db.TenantInfo?.Identifier ?? string.Empty;

        // Bail if this IAR was already landed. The marker below is written in the same SaveChanges as the
        // stock mutation, so a committed marker guarantees the stock was committed too (and vice-versa).
        var alreadyProcessed = await db.InboxMessages
            .AnyAsync(i => i.Id == @event.IARId && i.HandlerName == HandlerName, ct)
            .ConfigureAwait(false);
        if (alreadyProcessed)
        {
            logger.LogInformation(
                "[{Tenant}] Supply IAR {IARNumber} (IARId {IARId}) already landed into inventory; skipping redelivery.",
                tenantId, @event.IarNumber, @event.IARId);
            return;
        }

        var warehouseId = ExpendableModuleConstants.DefaultSupplyLocation.Id;

        var received = 0;
        var skipped = 0;
        var touchedProductIds = new HashSet<Guid>();

        foreach (var line in @event.AcceptedItems)
        {
            if (string.IsNullOrWhiteSpace(line.StockNumber))
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping Supply IAR {IARNumber} line '{Description}': no StockNumber on the line.",
                    tenantId, @event.IarNumber, line.Description);
                skipped++;
                continue;
            }

            var stockNo = line.StockNumber.Trim();
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.StockNo == stockNo && !p.IsDeleted, ct)
                .ConfigureAwait(false);

            if (product is null)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping Supply IAR {IARNumber} line '{Description}': no Expendable product with StockNo '{StockNo}'. " +
                    "The source PR must reference an existing product before acceptance.",
                    tenantId, @event.IarNumber, line.Description, stockNo);
                skipped++;
                continue;
            }

            var quantity = (int)Math.Floor(line.Quantity);
            if (quantity <= 0)
            {
                logger.LogWarning(
                    "[{Tenant}] Skipping Supply IAR {IARNumber} line '{Description}': non-positive quantity {Qty}.",
                    tenantId, @event.IarNumber, line.Description, line.Quantity);
                skipped++;
                continue;
            }

            var inventory = await db.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.WarehouseLocationId == warehouseId, ct)
                .ConfigureAwait(false);

            if (inventory is null)
            {
                inventory = ProductInventory.Create(tenantId, product.Id, warehouseId);
                db.ProductInventories.Add(inventory);
            }

            inventory.ReceiveFromPurchase(@event.IARId, product.Id, quantity, line.UnitCost, @event.IarNumber);
            touchedProductIds.Add(product.Id);
            received++;
        }

        if (received > 0)
        {
            // Commit the dedup marker atomically with the stock it accounts for. If two redeliveries race,
            // the composite PK (Id, HandlerName) rejects the second insert — its inventory write rolls back
            // with it, so no double-count; the retry then sees the committed marker and skips cleanly.
            db.InboxMessages.Add(new InboxMessage
            {
                Id = @event.IARId,
                HandlerName = HandlerName,
                EventType = nameof(SupplyIARAcceptedEvent),
                TenantId = tenantId,
                ProcessedOnUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        foreach (var productId in touchedProductIds)
            await cache.RemoveItemAsync($"inventory:{productId}:{warehouseId}", ct).ConfigureAwait(false);

        logger.LogInformation(
            "[{Tenant}] Expendable processed Supply IAR {IARNumber}: received={Received} skipped={Skipped} of {LineCount} accepted lines.",
            tenantId, @event.IarNumber, received, skipped, @event.AcceptedItems.Count);
    }
}
