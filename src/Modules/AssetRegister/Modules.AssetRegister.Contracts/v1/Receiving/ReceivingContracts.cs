using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

public sealed record ReceivingReportItemDto(
    Guid Id,
    Guid ReportId,
    Guid CatalogItemId,
    string? Reference,
    string PropertyNo,
    string Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount,
    string? SerialNo,
    string? Brand,
    string? Model,
    string? UacsObjectCode = null,
    string? SourceAgencyName = null,
    string? SourcePropertyNo = null,
    string? SourceDocumentRef = null,
    DateOnly? OriginalAcquisitionDate = null);

public sealed record ReceivingReportDto(
    Guid Id,
    ReceivingDocumentKind DocumentKind,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    EmployeeRefDto ReceivedBy,
    EmployeeRefDto? NotedBy,
    DateOnly? DateReceived,
    IReadOnlyCollection<ReceivingReportItemDto> Items);

public sealed record ReceivingReportSummaryDto(
    Guid Id,
    ReceivingDocumentKind DocumentKind,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    ReceiptType ReceiptType,
    int ItemCount,
    decimal TotalAmount,
    bool HasSignedCopy = false);

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// A single physical unit on a Receiving Report (one line = one asset).
/// <see cref="CatalogItemId"/> is required as of Phase 3 — the handler no longer falls back to fuzzy matching.
/// <see cref="PropertyNo"/> is inherited from the IAR acceptance phase — it is NOT generated here.
/// <see cref="UacsObjectCode"/> snapshots the PR-side Accountant-assigned UACS so the receiving record carries
/// its own copy independent of catalog mutations.
/// </summary>
public sealed record CreateReceivingReportItemRequest(
    Guid? CatalogItemId,
    string? Reference,
    string Description,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string PropertyNo,
    string? SerialNo,
    string? Brand,
    string? Model,
    Guid? SourceIARId = null,
    string? PropertyClassHint = null,
    string? UacsObjectCode = null,
    // ── Ad-hoc / external source (Donation, Transfer, Other) ───────────────────────────
    // Required when SourceIARId is null. SourceAgencyName must be set; the rest are optional but
    // strongly recommended for audit and depreciation continuity (COA GAM §V.B).
    string? SourceAgencyName = null,
    string? SourcePropertyNo = null,
    string? SourceDocumentRef = null,
    DateOnly? OriginalAcquisitionDate = null);

/// <summary>
/// Creates a Receiving Report (PPERR or SMRR) and materializes one AssetRegistry row
/// per line item. Each line represents exactly one physical unit; PropertyNo is
/// inherited from the IAR acceptance phase.
/// </summary>
public sealed record CreateReceivingReportCommand(
    ReceivingDocumentKind DocumentKind,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    EmployeeRefDto ReceivedBy,
    EmployeeRefDto? NotedBy,
    DateOnly? DateReceived,
    IReadOnlyList<CreateReceivingReportItemRequest> Items) : ICommand<ReceivingReportDto>;

public sealed record DeleteReceivingReportCommand(Guid Id) : ICommand<Unit>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetReceivingReportQuery(Guid Id) : IQuery<ReceivingReportDto?>;

public sealed record SearchReceivingReportsQuery(
    string? Keyword = null,
    ReceivingDocumentKind? DocumentKind = null,
    ReceiptType? ReceiptType = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<ReceivingReportSummaryDto>>;

/// <summary>
/// From the supplied candidate property numbers, returns the subset that already appears on an existing
/// Receiving Report (SMRR or PPERR) line — i.e. items that have already been received and must not be
/// received again. Backs the Receiving Report "From IAR" picker so already-received rows can be disabled
/// and suppliers whose every item is already received drop out of the supplier list.
/// </summary>
public sealed record GetReceivedPropertyNumbersQuery(
    IReadOnlyCollection<string> PropertyNumbers) : IQuery<IReadOnlyCollection<string>>;

/// <summary>
/// Previews the report number that would be assigned to a new receiving report of the given
/// kind/date, WITHOUT consuming it. Scoped to the current tenant. Backs the "next number"
/// preview on the create form. Best-effort — a concurrent create may change it before save.
/// </summary>
public sealed record PeekReceivingReportNumberQuery(
    ReceivingDocumentKind Kind,
    DateOnly Date) : IQuery<string>;

// ── PPERR Form Series ──────────────────────────────────────────────────────

public sealed record PPERRFormSeriesDto(
    Guid Id,
    int StartSerial,
    int EndSerial,
    int NextSerial,
    int Remaining,
    bool IsActive,
    bool IsExhausted,
    bool IsUnused);

public sealed record CreatePPERRFormSeriesCommand(
    int StartSerial,
    int EndSerial) : ICommand<PPERRFormSeriesDto>;

public sealed record UpdatePPERRFormSeriesCommand(
    Guid Id,
    int StartSerial,
    int EndSerial) : ICommand<PPERRFormSeriesDto>;

public sealed record DeletePPERRFormSeriesCommand(Guid Id) : ICommand<Unit>;

public sealed record ActivatePPERRFormSeriesCommand(Guid Id) : ICommand<PPERRFormSeriesDto>;

public sealed record DeactivatePPERRFormSeriesCommand(Guid Id) : ICommand<PPERRFormSeriesDto>;

public sealed record GetActivePPERRFormSeriesQuery : IQuery<PPERRFormSeriesDto?>;

public sealed record SearchPPERRFormSeriesQuery(
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<PPERRFormSeriesDto>>;

