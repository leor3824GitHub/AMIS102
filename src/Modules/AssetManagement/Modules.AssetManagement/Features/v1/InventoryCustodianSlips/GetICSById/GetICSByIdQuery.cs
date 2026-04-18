using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;

public sealed record GetICSByIdQuery(Guid Id) : IQuery<ICSDetailsDto>;

public sealed record ICSDetailsDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string Category,
    string Status,
    DateOnly? ExpiresOn,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    Guid? RenewedFromICSId,
    Guid? RenewedByICSId,
    Guid? CancelledByRRSPId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<ICSItemDetailsDto> Items);

public sealed record ICSItemDetailsDto(
    Guid Id,
    int ItemNo,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string? Description,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears,
    string AssetTypeAtTimeOfIssuance);
