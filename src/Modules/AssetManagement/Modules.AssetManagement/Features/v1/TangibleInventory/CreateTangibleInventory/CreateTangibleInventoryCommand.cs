using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;

public sealed record CreateTangibleInventoryCommand(
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid? NotedByEmployeeId,
    IReadOnlyList<CreateTangibleInventoryItemRequest> Items) : ICommand<CreateTangibleInventoryResult>;

/// <summary>
/// References a pre-registered TangibleItem. Snapshot data is sourced automatically
/// from that record — no redundant entry required.
/// </summary>
public sealed record CreateTangibleInventoryItemRequest(
    Guid TangibleItemId,
    string? Reference);

public sealed record CreateTangibleInventoryResult(
    Guid TangibleInventoryId,
    string ReportNo,
    int SEItemCount,
    int PPEItemCount);
