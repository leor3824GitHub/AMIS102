using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRById;

public sealed record GetSMRRByIdQuery(Guid Id) : IQuery<SMRRDetailsDto>;

public sealed record SMRRDetailsDto(
    Guid Id,
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    string ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid? NotedByEmployeeId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<SMRRItemDetailsDto> Items);

public sealed record SMRRItemDetailsDto(
    Guid Id,
    string? Reference,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount);
