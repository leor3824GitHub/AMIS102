using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPById;

public sealed record GetRRPByIdQuery(Guid Id) : IQuery<RRPDetailsDto>;

public sealed record RRPDetailsDto(
    Guid Id,
    string RRPNo,
    DateOnly Date,
    string ReturnCategory,
    Guid ReturnedByEmployeeId,
    Guid ApprovedByEmployeeId,
    Guid SignedByEmployeeId,
    bool PropertyInspectorCertified,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<RRPItemDto> Items);

public sealed record RRPItemDto(
    Guid Id,
    int ItemNo,
    Guid PPEItemId,
    string? SourceDocumentRef,
    string PropertyCode,
    string Description,
    int Quantity,
    decimal UnitCost,
    decimal TotalCost);
