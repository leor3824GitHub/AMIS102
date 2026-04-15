using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.PropertyHistory;

/// <summary>
/// Returns the complete lifecycle audit trail for a single semi-expendable property unit,
/// from initial receipt through every document it appears in.
/// </summary>
public sealed record GetPropertyHistoryQuery(Guid SemiExpendablePropertyId) : IQuery<PropertyHistoryDto>;

public sealed record PropertyHistoryDto(
    Guid PropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    string Category,
    decimal UnitCost,
    string CurrentStatus,
    Guid? CurrentCustodianId,
    IReadOnlyList<PropertyHistoryEventDto> Events);

public sealed record PropertyHistoryEventDto(
    DateOnly EventDate,
    string EventType,
    string DocumentType,
    string DocumentNo,
    string? Details);
