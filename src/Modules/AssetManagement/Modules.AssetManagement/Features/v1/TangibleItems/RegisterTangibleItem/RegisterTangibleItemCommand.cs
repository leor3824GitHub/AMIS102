using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;

public sealed record RegisterTangibleItemCommand(
    Guid ItemId,
    string PropertyNo,
    string PropertyClass,
    string CategoryCode,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks,
    Guid? PurchaseOrderId = null) : ICommand<TangibleItemDto>;

public sealed record TangibleItemDto(
    Guid Id,
    string PropertyNo,
    string PropertyClass,
    string CategoryCode,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks,
    Guid? PurchaseOrderId,
    DateTimeOffset CreatedOnUtc);
