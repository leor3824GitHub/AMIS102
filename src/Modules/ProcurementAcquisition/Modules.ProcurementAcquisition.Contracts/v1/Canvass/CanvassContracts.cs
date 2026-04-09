using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

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
    string? CreatedBy);

public sealed record CanvassRequestSummaryDto(
    Guid Id,
    string RivNumber,
    Guid PurchaseRequestId,
    string PrNumber,
    DateOnly ReturnDeadline,
    CanvassRequestStatus Status,
    int QuotationCount,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record CreateCanvassRequestCommand(
    Guid PurchaseRequestId,
    DateOnly ReturnDeadline) : ICommand<CanvassRequestDto>;

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

public sealed record AwardCanvassCommand(
    Guid CanvassRequestId,
    Guid AwardedQuotationId) : ICommand<CanvassRequestDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetCanvassRequestQuery(Guid Id) : IQuery<CanvassRequestDto?>;

public sealed class SearchCanvassRequestsQuery : IQuery<PagedResponse<CanvassRequestSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public CanvassRequestStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
