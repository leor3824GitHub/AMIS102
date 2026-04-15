using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;

public sealed record CreatePARCommand(
    string PARNo,
    DateOnly Date,
    PARType PARType,
    Guid ReceivedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    Guid ApprovedByEmployeeId,
    IReadOnlyList<CreatePARItemRequest> Items) : ICommand<CreatePARResult>;

public sealed record CreatePARItemRequest(
    Guid PPEItemId,
    int Quantity,
    string Unit,
    string ItemDescription);

public sealed record CreatePARResult(
    Guid PARId,
    string PARNo,
    int ItemCount);
