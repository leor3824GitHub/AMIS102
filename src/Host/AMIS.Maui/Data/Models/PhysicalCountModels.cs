using SQLite;

namespace Playground.Maui.Data.Models;

[Table("PendingCountEntries")]
public sealed class PendingCountEntry
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public string SessionId { get; set; } = "";
    public string PropertyNumber { get; set; } = "";
    // AssetRegister record-as-you-go fields.
    public string? AssetRegistryId { get; set; } // set for a recorded known asset; null for FoundAtStation
    public string LocationId { get; set; } = ""; // location where the item was counted (required)
    public string Article { get; set; } = "";    // snapshot article/description
    public string Unit { get; set; } = "unit";
    public string? Condition { get; set; }        // "Good" | "NeedsRepair" | etc. (known-asset records)
    public string? Remarks { get; set; }
    public bool IsScanned { get; set; }
    public bool IsFoundAtStation { get; set; }
    public string? ProposedCatalogItemId { get; set; } // set for FoundAtStation entries when operator picked a catalog item
    public decimal UnitCost { get; set; }
    public string SyncStatus { get; set; } = "Pending"; // "Pending" | "Failed"
    public DateTimeOffset CreatedAt { get; set; }
    public string? LastError { get; set; }
}
