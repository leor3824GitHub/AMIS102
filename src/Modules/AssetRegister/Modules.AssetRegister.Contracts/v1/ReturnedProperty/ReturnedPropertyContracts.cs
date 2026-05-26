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
    string ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    DateOnly Date,
    Guid AccountabilityId,
    string AccountabilityDocumentNo,
    EmployeeRefDto ReturnedBy,
    EmployeeRefDto? ReceivedBy,
    string? Remarks,
    IReadOnlyCollection<ReturnedPropertyReceiptItemDto> Items);

public sealed record ReturnedPropertyReceiptSummaryDto(
    Guid Id,
    string ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    DateOnly Date,
    string AccountabilityDocumentNo,
    int ItemCount,
    decimal TotalUnitCost);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreateReturnedPropertyReceiptCommand(
    string ReceiptNo,
    ReturnedPropertyReceiptType ReceiptType,
    DateOnly Date,
    Guid AccountabilityId,
    IReadOnlyList<Guid> AccountabilityLineIds,
    EmployeeRefDto ReturnedBy,
    EmployeeRefDto? ReceivedBy,
    string? Remarks) : ICommand<ReturnedPropertyReceiptDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetReturnedPropertyReceiptQuery(Guid Id) : IQuery<ReturnedPropertyReceiptDto?>;

public sealed record SearchReturnedPropertyReceiptsQuery(
    string? Keyword = null,
    ReturnedPropertyReceiptType? ReceiptType = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 15) : IQuery<PagedResponse<ReturnedPropertyReceiptSummaryDto>>;
