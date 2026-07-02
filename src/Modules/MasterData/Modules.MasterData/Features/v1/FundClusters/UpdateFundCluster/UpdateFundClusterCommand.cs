using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.UpdateFundCluster;

public sealed record UpdateFundClusterCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true) : ICommand;
