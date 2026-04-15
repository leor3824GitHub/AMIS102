using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRById;

public sealed record GetPPERRByIdQuery(Guid Id) : IQuery<PPERRDetailsDto>;

public sealed record PPERRDetailsDto(
    Guid Id,
    string PPERRNo,
    DateOnly Date,
    string ReceivedFrom,
    string Address,
    string ReceiptNature,
    Guid ReceivedByEmployeeId,
    Guid NotedByEmployeeId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PPERRItemDto> Items);

public sealed record PPERRItemDto(
    Guid Id,
    int ItemNo,
    string PropertyCode,
    string Description,
    DateOnly DateAcquired,
    int Quantity,
    decimal UnitCost,
    decimal Amount);
