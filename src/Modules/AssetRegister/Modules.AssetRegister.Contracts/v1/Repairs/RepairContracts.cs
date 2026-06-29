using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Repairs;

public static class RepairStatusValues
{
    public const string Requested = "Requested";
    public const string PreInspected = "PreInspected";
    public const string Repaired = "Repaired";
    public const string PostInspected = "PostInspected";
    public const string Accepted = "Accepted";
}

/// <summary>Full RPRI (Exhibit 6) projection — request, pre/post inspection, finance refs, acceptance.</summary>
public sealed record PropertyRepairDto(
    Guid Id,
    Guid AssetRegistryId,
    string RpriNo,
    string Status,
    // Description of property (resolved from the asset at read time)
    string PropertyNo,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    // Defects / complaints
    string NatureOfWork,
    string? PartsToReplace,
    string RequestedBy,
    DateOnly RequestedOn,
    string? EngineNo,
    string? ChassisNo,
    int? OdometerReading,
    string? NatureOfLastRepair,
    DateOnly? DateOfLastRepair,
    // Pre-repair inspection
    string? PreInspectionFindings,
    string? PreInspectedBy,
    string? NotedBy,
    DateOnly? PreInspectedOn,
    // Post-repair inspection
    string? RepairShop,
    string? JobOrderNo,
    string? InvoiceNo,
    DateOnly? InvoiceDate,
    decimal? AmountPerJO,
    string? PostInspectionFindings,
    string? PostInspectedBy,
    DateOnly? PostInspectedOn,
    // Procurement / finance refs (optional)
    string? PrNo,
    string? PoJoNo,
    string? BurNo,
    string? DvNo,
    // Acceptance
    string? AcceptedBy,
    DateOnly? AcceptedOn);

public sealed record PropertyRepairSummaryDto(
    Guid Id,
    Guid AssetRegistryId,
    string RpriNo,
    string Status,
    string PropertyNo,
    string Description,
    string NatureOfWork,
    DateOnly RequestedOn,
    DateOnly? AcceptedOn,
    decimal? AmountPerJO);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record RequestRepairCommand(
    Guid AssetRegistryId,
    string NatureOfWork,
    string RequestedBy,
    DateOnly RequestedOn,
    string? PartsToReplace = null,
    string? EngineNo = null,
    string? ChassisNo = null,
    int? OdometerReading = null) : ICommand<PropertyRepairDto>;

public sealed record RecordPreRepairInspectionCommand(
    Guid RepairId,
    string Findings,
    string PreInspectedBy,
    DateOnly PreInspectedOn,
    string? NotedBy = null) : ICommand<PropertyRepairDto>;

public sealed record RecordPostRepairInspectionCommand(
    Guid RepairId,
    string Findings,
    string PostInspectedBy,
    DateOnly PostInspectedOn,
    string? RepairShop = null,
    string? JobOrderNo = null,
    string? InvoiceNo = null,
    DateOnly? InvoiceDate = null,
    decimal? AmountPerJO = null,
    string? PrNo = null,
    string? PoJoNo = null,
    string? BurNo = null,
    string? DvNo = null) : ICommand<PropertyRepairDto>;

public sealed record AcceptRepairCommand(
    Guid RepairId,
    string AcceptedBy,
    DateOnly AcceptedOn) : ICommand<PropertyRepairDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetPropertyRepairQuery(Guid Id) : IQuery<PropertyRepairDto?>;

/// <summary>Repair history of an asset = its Accepted RPRIs, most recent first.</summary>
public sealed record GetAssetRepairHistoryQuery(Guid AssetRegistryId) : IQuery<List<PropertyRepairSummaryDto>>;

/// <summary>The asset's most recent accepted RPRI (drives "Nature/Date of Last Repair" auto-fill).</summary>
public sealed record GetLastRepairQuery(Guid AssetRegistryId) : IQuery<PropertyRepairDto?>;

public sealed record SearchPropertyRepairsQuery(
    Guid? AssetRegistryId = null,
    string? Status = null,
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyRepairSummaryDto>>;
