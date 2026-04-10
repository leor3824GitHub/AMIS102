using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;

public sealed record CreateSMRRCommand(
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    string? ReceivedBy,
    string? NotedBy,
    IReadOnlyList<CreateSMRRItemRequest> Items) : ICommand<CreateSMRRResult>;

public sealed record CreateSMRRItemRequest(
    string? Reference,
    Guid SemiExpendableItemId,
    string? Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost);

public sealed record CreateSMRRResult(
    Guid SMRRId,
    string SMRRNo,
    int PropertiesCreated);
