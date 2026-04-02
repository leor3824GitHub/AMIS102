using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Expendable.Contracts.v1.Purchases;

public record PurchaseLineItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    int ReceivedQuantity,
    int RejectedQuantity,
    int QuantityInspection);

public record PurchaseDto(
    Guid Id,
    string PurchaseOrderNumber,
    string SupplierId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    DateTimeOffset? DeliveryDate,
    string Status,
    decimal TotalAmount,
    string? ReceivingNotes,
    List<PurchaseLineItemDto> LineItems,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);

public record CreatePurchaseOrderCommand(
    string SupplierId,
    string SupplierName,
    Guid WarehouseLocationId,
    string WarehouseLocationName,
    DateTimeOffset? ExpectedDeliveryDate = null) : ICommand<PurchaseDto>;

public record AddPurchaseLineItemCommand(
    Guid PurchaseId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice) : ICommand<Unit>;

public record RemovePurchaseLineItemCommand(
    Guid PurchaseId,
    Guid ProductId) : ICommand<Unit>;

public record SubmitPurchaseOrderCommand(Guid Id) : ICommand<Unit>;

public record ApprovePurchaseOrderCommand(Guid Id) : ICommand<Unit>;

public record RecordPurchaseReceiptCommand(
    Guid PurchaseId,
    Guid ProductId,
    int ReceivedQuantity,
    int RejectedQuantity = 0,
    string? ReceivingNotes = null) : ICommand<Unit>;

public record CancelPurchaseOrderCommand(Guid Id) : ICommand<Unit>;

public record GetPurchaseQuery(Guid Id) : IQuery<PurchaseDto?>;

public sealed class SearchPurchasesQuery : IPagedQuery, IQuery<PagedResponse<PurchaseDto>>
{
    public string? PoNumber { get; set; }
    public string? Status { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetPurchasesBySupplierQuery : IPagedQuery, IQuery<PagedResponse<PurchaseDto>>
{
    public string? SupplierId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}


