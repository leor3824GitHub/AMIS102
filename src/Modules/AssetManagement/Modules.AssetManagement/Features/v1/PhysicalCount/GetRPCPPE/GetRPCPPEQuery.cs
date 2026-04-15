using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCPPE;

/// <summary>
/// Returns the Report on the Physical Count of PPE (RPCPPE) for a Physical Count Session.
/// Per COA Circular 2020-006 — PPE-only view enriched with depreciation data from PPEIR records.
/// Submitted by the Supply Officer to the Accounting Unit after the count is completed.
/// </summary>
public sealed record GetRPCPPEQuery(Guid SessionId) : IQuery<RPCPPEReportDto>;

public sealed record RPCPPEReportDto(
    Guid SessionId,
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    IReadOnlyList<RPCPPELineItemDto> Items,
    RPCPPESummaryDto Summary);

public sealed record RPCPPELineItemDto(
    int LineNo,
    /// <summary>PPE property code (e.g. IT Equipment).</summary>
    string PropertyCode,
    /// <summary>Full description including model, make, serial number.</summary>
    string Description,
    /// <summary>Property number assigned by Supply Officer.</summary>
    string PropertyNumber,
    DateOnly DateAcquired,
    /// <summary>Acquisition / unit cost.</summary>
    decimal UnitCost,
    /// <summary>Accumulated depreciation from the most recent PPEIR entry, if available.</summary>
    decimal? AccumulatedDepreciation,
    /// <summary>Net book value = UnitCost - AccumulatedDepreciation.</summary>
    decimal? BookValue,
    /// <summary>Quantity per property card (always 1 for individually tracked PPE).</summary>
    int QuantityPerCard,
    /// <summary>Quantity physically counted.</summary>
    int QuantityOnHand,
    int Shortage,
    int Overage,
    string? Condition,
    string? Remarks,
    string Result,
    bool IsScanned);

public sealed record RPCPPESummaryDto(
    int TotalItems,
    int Found,
    int NotFound,
    int FoundAtStation,
    int Pending,
    decimal TotalUnitCost,
    decimal? TotalAccumulatedDepreciation,
    decimal? TotalBookValue,
    int TotalShortage,
    int TotalOverage);
