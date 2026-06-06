using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;

public sealed record ReturnedPropertyReceiptItemDto(
    Guid Id,
    Guid ReceiptId,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    int ItemNo,
    AssetSnapshotDto Snapshot);

public sealed record ReturnedPropertyReceiptDto(
    Guid Id,
    string? ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    ReturnedPropertyReceiptStatus Status,
    DateOnly Date,
    Guid AccountabilityId,
    string AccountabilityDocumentNo,
    EmployeeRefDto ReturnedBy,
    EmployeeRefDto? ReceivedBy,
    string? Remarks,
    string? RejectionReason,
    string? CancellationReason,
    IReadOnlyCollection<ReturnedPropertyReceiptItemDto> Items);

public sealed record ReturnedPropertyReceiptSummaryDto(
    Guid Id,
    string? ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    ReturnedPropertyReceiptStatus Status,
    DateOnly Date,
    string AccountabilityDocumentNo,
    int ItemCount,
    decimal TotalUnitCost);

public sealed record ReturnedPropertyStatusCountDto(
    ReturnedPropertyReceiptStatus Status,
    int Count);

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>End-user raises a return request. Creates the receipt in <see cref="ReturnedPropertyReceiptStatus.Pending"/>
/// with no side-effects on assets or the accountability; the official receipt number is assigned on acceptance.</summary>
public sealed record CreateReturnedPropertyReceiptCommand(
    ReturnedPropertyReceiptType ReceiptType,
    DateOnly Date,
    Guid AccountabilityId,
    IReadOnlyList<Guid> AccountabilityLineIds,
    EmployeeRefDto ReturnedBy,
    string? Remarks) : ICommand<ReturnedPropertyReceiptDto>;

/// <summary>Property custodian receives/accepts a pending return. Flips each returned asset back to Available,
/// closes the accountability lines, assigns the official receipt number, and captures the receiver.</summary>
public sealed record AcceptReturnedPropertyReceiptCommand(
    Guid Id,
    EmployeeRefDto ReceivedBy) : ICommand<ReturnedPropertyReceiptDto>;

public sealed record RejectReturnedPropertyReceiptCommand(
    Guid Id,
    string Reason) : ICommand<ReturnedPropertyReceiptDto>;

public sealed record CancelReturnedPropertyReceiptCommand(
    Guid Id,
    string? Reason) : ICommand<ReturnedPropertyReceiptDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetReturnedPropertyReceiptQuery(Guid Id) : IQuery<ReturnedPropertyReceiptDto?>;

public sealed record SearchReturnedPropertyReceiptsQuery(
    string? Keyword = null,
    ReturnedPropertyReceiptType? ReceiptType = null,
    ReturnedPropertyReceiptStatus? Status = null,
    Guid? ReturnedByEmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 15) : IQuery<PagedResponse<ReturnedPropertyReceiptSummaryDto>>;

public sealed record GetReturnedPropertyStatusCountsQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    Guid? ReturnedByEmployeeId = null) : IQuery<IReadOnlyList<ReturnedPropertyStatusCountDto>>;
