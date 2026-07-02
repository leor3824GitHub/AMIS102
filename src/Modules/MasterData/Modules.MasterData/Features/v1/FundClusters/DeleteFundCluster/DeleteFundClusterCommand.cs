using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.DeleteFundCluster;

public sealed record DeleteFundClusterCommand(Guid Id) : ICommand;
