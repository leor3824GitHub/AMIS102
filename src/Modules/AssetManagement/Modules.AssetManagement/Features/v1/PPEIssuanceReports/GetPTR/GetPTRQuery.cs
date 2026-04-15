using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

/// <summary>
/// Property Transfer Report (PTR) — a read-only report derived from PPEIR data.
/// No separate entity: PTR fields map directly from PPEIssuanceReport.
/// </summary>
public sealed record GetPTRQuery(Guid PPEIRId) : IQuery<PTRDto>;

// PTR Number = PPEIR Number (PTR is a report derived from PPEIR — no separate entity)
public sealed record PTRDto(
    string PTRNo,
    DateOnly Date,
    Guid FromAccountableOfficerId,
    Guid ToAccountableOfficerId,
    string ToOfficeAddress,
    string TransferType,
    Guid ApprovedByEmployeeId,
    Guid ReleasedByEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<PTRItemDto> Items);

public sealed record PTRItemDto(
    int ItemNo,
    DateOnly DateAcquired,
    string PropertyNumber,
    string Description,
    decimal Amount,
    string? Condition,
    string? ReasonForTransfer);
