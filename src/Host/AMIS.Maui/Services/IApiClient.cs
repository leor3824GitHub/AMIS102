using AMIS.Maui.Data.Models;

namespace AMIS.Maui.Services;

// DTOs for API responses
public sealed record TokenIssueRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken);
public sealed record UserProfileDto(string Id, string Email, string? FirstName, string? LastName, string? ImageUrl);
public sealed record MyEmployeeDto(Guid EmployeeId, string FullName, string? Department, string? Position);

public sealed record ICSSummaryDto(
    Guid Id,
    string ICSNo,
    string Date,
    string Status,
    string? ExpiresOn,
    int ItemCount);

public sealed record PARSummaryDto(
    Guid Id,
    string PARNo,
    string Date,
    string PARType,
    int ItemCount);

public sealed record ICSDetailDto(
    Guid Id,
    string ICSNo,
    string Date,
    string Status,
    string? ExpiresOn,
    string FundCluster,
    List<ICSItemDto> Items);

public sealed record ICSItemDto(
    Guid Id,
    string PropertyNo,
    string? Description,
    string AssetType,
    string Unit,
    decimal UnitCost,
    int EstimatedUsefulLifeYears,
    string DateAcquired);

public sealed record PARDetailDto(
    Guid Id,
    string PARNo,
    string Date,
    string PARType,
    string Status,
    string FundCluster,
    List<PARItemDto> Items);

public sealed record PARItemDto(
    Guid Id,
    string PropertyNo,
    string ItemDescription,
    string AssetType,
    string Unit,
    decimal UnitCost,
    int Quantity,
    int EstimatedUsefulLifeYears,
    string DateAcquired)
{
    // Extended-line value (unit cost × quantity issued) — computed for display only.
    public decimal TotalCost => UnitCost * Quantity;

    // "2 unit" / "1 piece" — pairs the issued quantity with its unit of measure.
    public string QuantityDisplay => $"{Quantity} {Unit}".TrimEnd();

    // Only show the extended total when it differs from the unit cost shown above
    // (i.e. quantity > 1). For single-quantity lines it's redundant with UnitCost.
    public bool ShowTotal => Quantity > 1;
}

public sealed record TangibleInventoryItemDetailDto(
    Guid Id,
    string PropertyNo,
    string ItemName,
    string? Description,
    decimal UnitCost,
    string AssetType,
    bool IsIssued,
    string? LinkedDocumentType,
    string? LinkedDocumentNo,
    Guid? LinkedDocumentId,
    string Unit = "unit",
    Guid? CurrentLocationId = null,
    string? SerialNo = null,
    DateOnly? AcquisitionDate = null,
    string? LocationName = null,
    string? AccountableOfficer = null,
    string? AccountableOfficerDesignation = null);

// ── Physical Count ────────────────────────────────────────────────────────────

public sealed record PhysicalCountSessionSummaryDto(
    Guid Id,
    string SessionNo,
    DateOnly CountDate,
    string FundCluster,
    string Scope,
    string Status,
    int TotalEntries,
    int Found,
    int Missing,
    int FoundAtStation)
{
    // Recording is only permitted while the session is Ongoing (server enforces the same).
    public bool IsOngoing => string.Equals(Status, "Ongoing", StringComparison.OrdinalIgnoreCase);

    // A Closed session is signed off — nothing left to count, so it opens the read-only review.
    // Everything else (Ongoing, Draft, Reconciled, or an unexpected value) opens the Scan screen so
    // the scan-to-add UI is always reachable.
    public bool IsReviewOnly => string.Equals(Status, "Closed", StringComparison.OrdinalIgnoreCase);
}

public sealed record PhysicalCountSessionDetailDto(
    Guid Id,
    string SessionNo,
    DateOnly CountDate,
    string FundCluster,
    string Scope,
    string Status,
    List<PhysicalCountEntryDto> Entries);

public sealed record PhysicalCountEntryDto(
    Guid Id,
    Guid? TangibleInventoryItemId,
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    string? Result,      // "Found" | "NotFound" | "FoundAtStation" | null
    string? Condition,   // "Good" | "NeedsRepair" | etc. | null
    int QuantityOnHand,
    string? Remarks,
    bool IsScanned);

// Enums are serialized as strings (JsonStringEnumConverter is configured globally).
// AssetRegister records a found asset by AssetRegistryId + condition at a location
// (record-as-you-go; there is no pre-generated checklist to mark).
public sealed record RecordCountEntryRequest(
    Guid AssetRegistryId,
    string Article,
    string Unit,
    decimal UnitCost,
    string Condition,
    Guid LocationId,
    string? Remarks,
    bool IsScanned);

public sealed record CatalogItemDto(Guid Id, string Code, string Description, string DefaultUnit);

// Lightweight asset match returned by a serial-number search (serials aren't unique, so a search
// can return several). PropertyNo lets the caller fall back into the normal property-number flow.
public sealed record AssetSummaryDto(Guid Id, string PropertyNo, string AssetType, string Description, decimal UnitCost);

public sealed record AddFoundAtStationRequest(
    string PropertyNumber,
    string Description,
    string Unit,
    decimal UnitCost,
    Guid LocationId,
    string? Remarks,
    Guid? ProposedCatalogItemId = null);

public sealed record AddFoundAtStationResult(
    Guid EntryId,
    string PropertyNumber);

// Location reference for the "counting at" picker (shared master data).
public sealed record LocationDto(Guid Id, string Code, string Name);

// ─────────────────────────────────────────────────────────────────────────────

public interface IApiClient
{
    Task<TokenResponse> IssueTokenAsync(string email, string password, CancellationToken ct = default);
    Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default);
    Task<MyEmployeeDto> GetMyEmployeeAsync(CancellationToken ct = default);
    Task<List<ICSSummaryDto>> GetMyICSListAsync(Guid employeeId, CancellationToken ct = default);
    Task<ICSDetailDto> GetICSByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PARSummaryDto>> GetMyPARListAsync(Guid employeeId, CancellationToken ct = default);
    Task<PARDetailDto> GetPARByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Accepts a pending ICS/PAR issued to the current employee (PendingAcceptance → Active).</summary>
    Task AcceptAccountabilityAsync(Guid id, CancellationToken ct = default);

    Task<TangibleInventoryItemDetailDto> GetItemByPropertyNoAsync(string propertyNo, CancellationToken ct = default);

    /// <summary>Finds assets whose serial number matches (case-insensitive). May return 0, 1, or many.</summary>
    Task<IReadOnlyList<AssetSummaryDto>> SearchAssetsBySerialAsync(string serialNo, CancellationToken ct = default);

    Task<List<CatalogItemDto>> SearchCatalogItemsAsync(string keyword, CancellationToken ct = default);
    Task<List<LocationDto>> GetLocationsAsync(CancellationToken ct = default);
    Task<List<PhysicalCountSessionSummaryDto>> GetPhysicalCountSessionsAsync(CancellationToken ct = default);
    Task<PhysicalCountSessionDetailDto> GetPhysicalCountSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task RecordPhysicalCountEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default);
    Task<AddFoundAtStationResult> AddFoundAtStationEntryAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default);

    // ── Chat ──
    Task<List<ChatChannelDto>> GetChatChannelsAsync(CancellationToken ct = default);
    Task<ChatMessagePageDto> GetChatMessagesAsync(Guid channelId, Guid? before, int? take, CancellationToken ct = default);
    Task<ChatMessageDto?> SendChatMessageAsync(Guid channelId, string content, CancellationToken ct = default);
}
