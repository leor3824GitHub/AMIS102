using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.RecordPhysicalCountEntry;

public sealed record RecordPhysicalCountEntryCommand(
    Guid SessionId,
    Guid EntryId,
    PhysicalCountEntryResult Result,
    PhysicalCountCondition? Condition,
    int QuantityOnHand,
    string? Remarks,
    /// <summary>True when this record was captured via camera scan.</summary>
    bool IsScanned,
    /// <summary>Storage path/key of the captured sticker photo. Populated when IsScanned is true.</summary>
    string? PhotoPath) : ICommand<RecordPhysicalCountEntryResult>;

public sealed record RecordPhysicalCountEntryResult(
    Guid EntryId,
    string PropertyNumber,
    string Result,
    string? Condition,
    bool IsScanned);
