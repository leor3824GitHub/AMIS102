using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Domain.Inventory;
using AMIS.Modules.Expendable.Domain.Purchases;
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

    internal static RejectedInventoryDto ToRejectedInventoryDto(this RejectedInventory rejected) =>
        new(
            rejected.Id,
            rejected.PurchaseId,
            rejected.ProductId,
            rejected.ProductCode ?? string.Empty,
            rejected.ProductName ?? string.Empty,
            rejected.WarehouseLocationId,
            rejected.WarehouseLocationName ?? string.Empty,
            rejected.QuantityRejected,
            rejected.UnitPrice,
            rejected.TotalValue,
            rejected.RejectionReason ?? string.Empty,
            rejected.Notes,
            rejected.Status.ToString(),
            rejected.RejectionDate,
            rejected.DispositionDate,
            rejected.DispositionNotes
        );

    internal static PurchaseInspectionDto ToPurchaseInspectionDto(this PurchaseInspection inspection) =>
        new(
            inspection.Id,
            inspection.PurchaseId,
            inspection.ProductId,
            inspection.QuantityReceivedForInspection,
            inspection.QuantityAccepted,
            inspection.QuantityRejected,
            inspection.Status.ToString(),
            inspection.RejectionReason ?? string.Empty,
            inspection.Notes,
            inspection.InspectionDate,
            inspection.Defects.Select(d => new InspectionDefectDto(
                d.UnitNumber,
                d.DefectDescription ?? string.Empty,
                d.Severity
            )).ToList()
        );
}

