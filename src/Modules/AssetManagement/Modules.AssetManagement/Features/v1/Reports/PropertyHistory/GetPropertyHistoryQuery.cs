using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.PropertyHistory;

/// <summary>
/// Returns the complete lifecycle audit trail for a single tangible inventory item,
/// from initial receipt through every downstream document it appears in.
/// </summary>
public sealed record GetPropertyHistoryQuery(Guid TangibleInventoryItemId) : IQuery<PropertyHistoryDto>;

public sealed record PropertyHistoryDto(
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string AssetType,
    decimal UnitCost,
    decimal ThresholdAmountUsed,
    bool IsIssued,
    Guid? CurrentCustodianId,
    IReadOnlyList<PropertyHistoryEventDto> Events);

public sealed record PropertyHistoryEventDto(
    DateOnly EventDate,
    string EventType,
    string DocumentType,
    string DocumentNo,
    string? Details);

