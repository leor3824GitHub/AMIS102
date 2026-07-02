using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusterById;

public sealed record GetFundClusterByIdQuery(Guid Id) : IQuery<FundClusterDetailsDto>;

public sealed record FundClusterDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);
