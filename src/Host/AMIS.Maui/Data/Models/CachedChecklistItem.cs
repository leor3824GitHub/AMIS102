using SQLite;

namespace AMIS.Maui.Data.Models;

// Offline snapshot of one session's coverage-checklist row. The whole in-scope set is cached per
// session so field staff can see "what's left to count" with no signal (stale-while-revalidate).
// Enums are stored as their string form to match the API JSON.
[Table("CachedChecklistItems")]
public sealed class CachedChecklistItem
{
    // sessionId + "|" + assetRegistryId — one row per asset per session.
    [PrimaryKey] public string LocalKey { get; set; } = "";
    [Indexed] public string SessionId { get; set; } = "";
    public string AssetRegistryId { get; set; } = "";
    public string PropertyNo { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitCost { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? CustodianId { get; set; }
    public string? AccountableOfficer { get; set; }
    public string Status { get; set; } = "";
    public string? Condition { get; set; }
    // Whether the asset has a photo. The bytes are fetched on demand from the image endpoint, never
    // cached inline here (a photo per row would bloat the offline DB).
    public bool HasImage { get; set; }
    public DateTimeOffset CachedAt { get; set; }
}
