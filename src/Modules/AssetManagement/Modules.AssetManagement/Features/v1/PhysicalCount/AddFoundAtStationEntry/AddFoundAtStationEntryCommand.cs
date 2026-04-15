using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.AddFoundAtStationEntry;

/// <summary>
/// Adds a "Found at Station" entry for an asset discovered during the physical count
/// that was NOT on the pre-generated checklist (unrecorded or mis-assigned asset).
/// </summary>
public sealed record AddFoundAtStationEntryCommand(
    Guid SessionId,
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    PhysicalCountCondition Condition,
    string? Remarks,
    string? PhotoPath) : ICommand<AddFoundAtStationEntryResult>;

public sealed record AddFoundAtStationEntryResult(
    Guid EntryId,
    string PropertyNumber);
