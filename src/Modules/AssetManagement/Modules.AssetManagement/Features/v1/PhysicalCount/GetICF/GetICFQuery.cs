using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetICF;

/// <summary>
/// Returns the Inventory Count Form (ICF) for a submitted Physical Count Session.
/// Per COA Circular 2020-006 — the ICF is the working document submitted to the Accounting Unit.
/// </summary>
public sealed record GetICFQuery(Guid SessionId) : IQuery<ICFReportDto>;

public sealed record ICFReportDto(
    Guid SessionId,
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    string Scope,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    IReadOnlyList<ICFLineItemDto> Items);

public sealed record ICFLineItemDto(
    int LineNo,
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    int QuantityPerCard,
    int QuantityOnHand,
    int Shortage,
    int Overage,
    string? Condition,
    string? Remarks,
    string Result,
    bool IsScanned);

