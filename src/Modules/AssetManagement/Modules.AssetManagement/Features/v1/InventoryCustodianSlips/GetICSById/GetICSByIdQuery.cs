using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;

public sealed record GetICSByIdQuery(Guid Id) : IQuery<ICSDetailsDto>;

public sealed record ICSDetailsDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<ICSItemDetailsDto> Items);

public sealed record ICSItemDetailsDto(
    Guid Id,
    int ItemNo,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    string? Description,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears);
