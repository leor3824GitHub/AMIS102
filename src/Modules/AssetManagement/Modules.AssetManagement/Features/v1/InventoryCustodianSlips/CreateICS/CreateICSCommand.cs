using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public sealed record CreateICSCommand(
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<CreateICSItemRequest> Items) : ICommand<CreateICSResult>;

public sealed record CreateICSItemRequest(
    Guid SemiExpendablePropertyId,
    string? Description);

public sealed record CreateICSResult(
    Guid ICSId,
    string ICSNo,
    int ItemCount);
