using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.UpdateTangibleInventory;

public sealed record UpdateTangibleInventoryCommand(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid? NotedByEmployeeId) : ICommand<Unit>;

