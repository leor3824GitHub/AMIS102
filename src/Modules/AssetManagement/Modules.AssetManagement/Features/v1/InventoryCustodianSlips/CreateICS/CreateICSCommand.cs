using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public sealed record CreateICSCommand(
    string ICSNo,
    DateOnly Date,
    AssetCategory Category,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<CreateICSItemRequest> Items) : ICommand<CreateICSResult>;

public sealed record CreateICSItemRequest(
    Guid TangibleInventoryItemId,
    string? Description);

public sealed record CreateICSResult(
    Guid ICSId,
    string ICSNo,
    int ItemCount);

