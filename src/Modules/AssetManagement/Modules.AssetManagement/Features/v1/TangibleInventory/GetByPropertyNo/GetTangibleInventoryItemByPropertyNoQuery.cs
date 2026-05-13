using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.GetByPropertyNo;

public sealed record GetTangibleInventoryItemByPropertyNoQuery(string PropertyNo)
    : IQuery<TangibleInventoryItemDetailDto>;

public sealed record TangibleInventoryItemDetailDto(
    Guid Id,
    string PropertyNo,
    string ItemName,
    string? Description,
    decimal UnitCost,
    string AssetType,
    bool IsIssued,
    string? LinkedDocumentType,
    string? LinkedDocumentNo,
    Guid? LinkedDocumentId);

