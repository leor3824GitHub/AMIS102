using SQLite;

namespace Playground.Maui.Data.Models;

[Table("PendingCountEntries")]
public sealed class PendingCountEntry
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public string SessionId { get; set; } = "";
    public string? EntryId { get; set; }        // null for FoundAtStation entries
    public string PropertyNumber { get; set; } = "";
    public string Result { get; set; } = "";    // "Found" | "NotFound" | "FoundAtStation"
    public string? Condition { get; set; }      // "Good" | "NeedsRepair" | etc.
    public int QuantityOnHand { get; set; } = 1;
    public string? Remarks { get; set; }
    public bool IsScanned { get; set; }
    public bool IsFoundAtStation { get; set; }
    public string? Description { get; set; }   // required for FoundAtStation
    public decimal UnitCost { get; set; }      // required for FoundAtStation
    public string SyncStatus { get; set; } = "Pending"; // "Pending" | "Failed"
    public DateTimeOffset CreatedAt { get; set; }
    public string? LastError { get; set; }
}
