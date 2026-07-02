using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.CreateFundCluster;

public sealed record CreateFundClusterCommand(
    string Code,
    string Name,
    string? Description = null) : ICommand<FundClusterDto>;

public sealed record FundClusterDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);
