using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum CanvassRequestStatus
{
    Open = 0,
    Evaluated = 1,
    Awarded = 2,
    Cancelled = 3
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record CanvassQuotationLineItemDto(
    int ItemNo,
    int PrItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record CanvassQuotationDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? TinNumber,
    DateOnly QuotationDate,
    string? DeliveryTerms,
    bool IsAwarded,
    IReadOnlyList<CanvassQuotationLineItemDto> LineItems);

/// <summary>A Purchase Request line item covered by a canvass (snapshot taken at canvass creation).
/// The <c>Awarded*</c> fields carry the per-line split-award winner; null until the canvass is awarded.</summary>
public sealed record CanvassLineItemDto(
    int PrItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotalCost,
    Guid? AwardedQuotationId = null,
    Guid? AwardedSupplierId = null,
    string? AwardedSupplierName = null,
    decimal? AwardedUnitPrice = null);

/// <summary>A purchase order spawned from a canvass — one per winning supplier under split award.</summary>
public sealed record CanvassPurchaseOrderRefDto(
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status);

/// <summary>A Purchase Request line with its current canvass-coverage status, used to populate the canvass create form.</summary>
public sealed record CanvassablePrLineDto(
    int ItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal EstimatedUnitCost,
    bool IsCovered,
    string? CoveringRivNumber);

/// <summary>One ROPC committee signatory frozen at award time (Abstract of Canvass faithful reprint).</summary>
public sealed record CanvassAwardSignatoryDto(int SortOrder, string Name, string Role);

public sealed record CanvassRequestDto(
    Guid Id,
    string RivNumber,
    Guid PurchaseRequestId,
    string PrNumber,
    DateOnly ReturnDeadline,
    CanvassRequestStatus Status,
    Guid? AwardedSupplierId,
    string? AwardedSupplierName,
    IReadOnlyList<CanvassQuotationDto> Quotations,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<CanvassLineItemDto> LineItems,
    bool HasPurchaseOrder = false,
    string? PurchaseOrderNumber = null,
    IReadOnlyList<CanvassAwardSignatoryDto>? AwardSignatories = null,
    // Every PO spawned from this canvass. Under split award there is one per winning supplier;
    // HasPurchaseOrder / PurchaseOrderNumber remain populated from the first for back-compat.
    IReadOnlyList<CanvassPurchaseOrderRefDto>? PurchaseOrders = null);

public sealed record CanvassRequestSummaryDto(
    Guid Id,
    string RivNumber,
    Guid PurchaseRequestId,
    string PrNumber,
    DateOnly ReturnDeadline,
    CanvassRequestStatus Status,
    int QuotationCount,
    int LineItemCount,
    DateTimeOffset CreatedOnUtc,
    bool HasPurchaseOrder = false,
    string? PurchaseOrderNumber = null,
    bool HasSignedCopy = false,
    int PurchaseOrderCount = 0);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record CreateCanvassRequestCommand(
    Guid PurchaseRequestId,
    DateOnly ReturnDeadline,
    IReadOnlyList<int> PrItemNos) : ICommand<CanvassRequestDto>;

public sealed record AddQuotationLineItemRequest(
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice);

public sealed record AddQuotationCommand(
    Guid CanvassRequestId,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? TinNumber,
    DateOnly QuotationDate,
    string? DeliveryTerms,
    IReadOnlyList<AddQuotationLineItemRequest> LineItems) : ICommand<CanvassQuotationDto>;

public sealed record UpdateQuotationCommand(
    Guid QuotationId,
    string SupplierName,
    string SupplierAddress,
    string? TinNumber,
    DateOnly QuotationDate,
    string? DeliveryTerms,
    IReadOnlyList<AddQuotationLineItemRequest> LineItems) : ICommand<CanvassQuotationDto>;

/// <summary>One line of a split award: the covered PR line and the quotation chosen to win it.</summary>
public sealed record CanvassLineAwardRequest(
    int PrItemNo,
    Guid QuotationId);

/// <summary>
/// Awards a canvass line-by-line. Every covered line must appear exactly once; different lines may go to
/// different suppliers, which is what lets the canvass fan out into one purchase order per winning supplier.
/// </summary>
public sealed record AwardCanvassCommand(
    Guid CanvassRequestId,
    IReadOnlyList<CanvassLineAwardRequest> LineAwards) : ICommand<CanvassRequestDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetCanvassRequestQuery(Guid Id) : IQuery<CanvassRequestDto?>;

/// <summary>Returns every line of a PR with its current canvass-coverage status (for the canvass create form).</summary>
public sealed record GetCanvassablePrLinesQuery(Guid PurchaseRequestId) : IQuery<IReadOnlyList<CanvassablePrLineDto>>;

public sealed class SearchCanvassRequestsQuery : IQuery<PagedResponse<CanvassRequestSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public CanvassRequestStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

