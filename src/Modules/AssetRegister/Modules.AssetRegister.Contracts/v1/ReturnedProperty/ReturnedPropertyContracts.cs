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
    AssetSnapshotDto Snapshot,
    AssetCondition? InspectedCondition = null);

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
    IReadOnlyCollection<ReturnedPropertyReceiptItemDto> Items,
    EmployeeRefDto? InspectedBy = null,
    string? InspectionRemarks = null,
    EmployeeRefDto? AssignedInspector = null);

public sealed record ReturnedPropertyReceiptSummaryDto(
    Guid Id,
    string? ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    ReturnedPropertyReceiptStatus Status,
    DateOnly Date,
    string AccountabilityDocumentNo,
    int ItemCount,
    decimal TotalUnitCost,
    Guid AssignedInspectorEmployeeId = default);

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
    EmployeeRefDto Inspector,
    string? Remarks) : ICommand<ReturnedPropertyReceiptDto>;

/// <summary>One inspected line: the returned item and the condition the inspector assessed it at.</summary>
public sealed record ReturnedPropertyInspectionItemDto(
    Guid ItemId,
    AssetCondition Condition);

/// <summary>Inspector assesses a pending return, recording each returned item's actual condition.
/// Moves the request to <see cref="ReturnedPropertyReceiptStatus.Inspected"/>; no asset state changes yet.
/// The inspector is resolved server-side from the authenticated identity (never client-supplied) and must
/// match the inspector nominated on the request.</summary>
public sealed record InspectReturnedPropertyReceiptCommand(
    Guid Id,
    IReadOnlyList<ReturnedPropertyInspectionItemDto> ItemConditions,
    string? Remarks) : ICommand<ReturnedPropertyReceiptDto>;

/// <summary>Property custodian receives/accepts an inspected return. Flips each returned asset back to Available
/// at its inspected condition, closes the accountability lines, assigns the official receipt number, and captures the receiver.</summary>
public sealed record AcceptReturnedPropertyReceiptCommand(
    Guid Id,
    EmployeeRefDto ReceivedBy) : ICommand<ReturnedPropertyReceiptDto>;

/// <summary>Requester replaces the inspector they nominated, while the request is still Pending
/// (before anyone inspects it). Uses the same permission as creating/withdrawing a request.</summary>
public sealed record ReassignReturnedPropertyInspectorCommand(
    Guid Id,
    EmployeeRefDto Inspector) : ICommand<ReturnedPropertyReceiptDto>;

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
