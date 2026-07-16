using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Domain.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse;

internal static class WarehouseMapper
{
    /// <summary>
    /// Maps an inventory row to its DTO. Product code/name and the warehouse name are supplied by the caller
    /// (joined from <c>Product</c> / resolved from the location constant) — they are no longer denormalized
    /// onto <see cref="ProductInventory"/>, so the DTO always reflects the live product name (no rename drift).
    /// </summary>
    internal static ProductInventoryDto ToProductInventoryDto(
        this ProductInventory inventory, string productCode, string productName, string warehouseName) =>
        new(
            inventory.Id,
            inventory.ProductId,
            productCode,
            productName,
            inventory.WarehouseLocationId,
            warehouseName,
            inventory.QuantityAvailable,
            inventory.QuantityReserved,
            inventory.QuantityOnHand,
            inventory.QuantityIssued,
            inventory.TotalValue,
            inventory.ReservedValue,
            inventory.AverageUnitPrice,
            inventory.Status.ToString(),
            inventory.FirstReceiptDate,
            inventory.LastReceiptDate,
            inventory.LastIssueDate
        );
}
