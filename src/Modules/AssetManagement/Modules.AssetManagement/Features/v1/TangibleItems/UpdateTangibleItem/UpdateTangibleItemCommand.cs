using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.UpdateTangibleItem;

public sealed record UpdateTangibleItemCommand(
    Guid Id,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks,
    Guid? PurchaseOrderId = null) : ICommand<TangibleItemDto>;
