using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.SemiExpendablePropertyCard;

/// <summary>
/// Generates a Semi-expendable Property Card (SPC) for a specific catalog item type.
/// Shows all stock movement (receipts via SMRR, issuances via ICS, returns via RRSP,
/// transfers via SMIR, incidents via RLSDDSP, disposals via IIRUSP)
/// with a running on-hand balance.
/// </summary>
public sealed record GetSPCQuery(
    Guid ItemId,
    DateOnly? DateFrom,
    DateOnly? DateTo) : IQuery<SPCDto>;

public sealed record SPCDto(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    IReadOnlyList<SPCEntryDto> Entries);

public sealed record SPCEntryDto(
    DateOnly Date,
    string DocumentType,
    string DocumentNo,
    int QuantityIn,
    int QuantityOut,
    decimal UnitCost,
    int RunningBalance,
    string? Remarks);
