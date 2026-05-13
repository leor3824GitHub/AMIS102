using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPById;

public sealed record GetRRSPByIdQuery(Guid Id) : IQuery<RRSPDetailsDto>;

public sealed record RRSPDetailsDto(
    Guid Id,
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string ICSNo,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<RRSPItemDetailsDto> Items);

public sealed record RRSPItemDetailsDto(
    Guid Id,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string AssetTypeAtTimeOfReturn);

