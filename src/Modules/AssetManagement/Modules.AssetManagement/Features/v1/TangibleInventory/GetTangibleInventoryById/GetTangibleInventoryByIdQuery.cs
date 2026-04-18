using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventoryById;

public sealed record GetTangibleInventoryByIdQuery(Guid Id) : IQuery<TangibleInventoryDetailDto>;

public sealed record TangibleInventoryDetailDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    string ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid? NotedByEmployeeId,
    IReadOnlyList<TangibleInventoryItemDto> Items);

public sealed record TangibleInventoryItemDto(
    Guid Id,
    Guid TangibleItemId,
    string? Reference,
    string AssetType,
    decimal ThresholdAmountUsed,
    bool IsIssued,
    string PropertyNo,
    Guid ItemId,
    string? Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount);
