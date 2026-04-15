using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed record CreatePPERRCommand(
    string PPERRNo,
    DateOnly Date,
    string ReceivedFrom,
    string Address,
    PPEReceiptNature ReceiptNature,
    Guid ReceivedByEmployeeId,
    Guid NotedByEmployeeId,
    IReadOnlyList<CreatePPERRItemRequest> Items) : ICommand<CreatePPERRResult>;

public sealed record CreatePPERRItemRequest(
    string PropertyCode,
    string Description,
    string? SerialNumber,
    DateOnly DateAcquired,
    int Quantity,
    decimal UnitCost,
    int EstimatedUsefulLifeYears);

public sealed record CreatePPERRResult(
    Guid PPERRId,
    string PPERRNo,
    int PPEItemsCreated);
