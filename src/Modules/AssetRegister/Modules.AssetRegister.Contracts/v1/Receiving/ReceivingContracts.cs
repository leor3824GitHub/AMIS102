using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

public sealed record ReceivingReportItemDto(
    Guid Id,
    Guid ReportId,
    Guid CatalogItemId,
    string? Reference,
    string Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount,
    string? SerialNo,
    string? Brand,
    string? Model);

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
    decimal TotalAmount);

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// A single physical unit on a Receiving Report (one line = one asset).
/// <see cref="PropertyNo"/> is inherited from the IAR acceptance phase — it is NOT
/// generated here. When the source is an accepted IAR, also pass
/// <see cref="SourceIARId"/> + <see cref="PropertyClassHint"/> so the handler can
/// resolve the catalog entry automatically.
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
    string? PropertyClassHint = null);

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

