using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Domain.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse;

internal static class WarehouseMapper
{
    internal static ProductInventoryDto ToProductInventoryDto(this ProductInventory inventory) =>
        new(
            inventory.Id,
            inventory.ProductId,
            inventory.ProductCode ?? string.Empty,
            inventory.ProductName ?? string.Empty,
            inventory.WarehouseLocationId,
            inventory.WarehouseLocationName ?? string.Empty,
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
