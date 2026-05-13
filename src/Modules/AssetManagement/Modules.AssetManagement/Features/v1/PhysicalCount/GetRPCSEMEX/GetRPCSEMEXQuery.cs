using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCSEMEX;

/// <summary>
/// Returns the Report on the Physical Count of Semi-Expendable Properties (RPCSEMEX)
/// for a Physical Count Session. Per COA Circular 2020-006 — SE-only view.
/// Submitted by the Supply Officer to the Accounting Unit after the count is completed.
/// </summary>
public sealed record GetRPCSEMEXQuery(Guid SessionId) : IQuery<RPCSEMEXReportDto>;

public sealed record RPCSEMEXReportDto(
    Guid SessionId,
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    IReadOnlyList<RPCSEMEXLineItemDto> Items,
    RPCSEMEXSummaryDto Summary);

public sealed record RPCSEMEXLineItemDto(
    int LineNo,
    string PropertyCode,
    string Description,
    string PropertyNumber,
    DateOnly DateAcquired,
    decimal UnitCost,
    int QuantityPerCard,
    int QuantityOnHand,
    int Shortage,
    int Overage,
    string? Condition,
    string? Remarks,
    string Result,
    bool IsScanned);

public sealed record RPCSEMEXSummaryDto(
    int TotalItems,
    int Found,
    int NotFound,
    int FoundAtStation,
    int Pending,
    decimal TotalUnitCost,
    int TotalShortage,
    int TotalOverage);
