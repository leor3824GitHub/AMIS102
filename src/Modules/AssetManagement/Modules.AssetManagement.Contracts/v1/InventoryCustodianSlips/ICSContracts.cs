using Mediator;

namespace AMIS.Modules.AssetManagement.Contracts.v1.InventoryCustodianSlips;

public sealed record GetICSForPrintQuery(Guid Id) : IQuery<ICSForPrintDto?>;

public sealed record ICSForPrintDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<ICSItemForPrintDto> Items);

public sealed record ICSItemForPrintDto(
    int ItemNo,
    string PropertyNo,
    string UnitOfMeasure,
    string? Description,
    string ItemName,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears);
