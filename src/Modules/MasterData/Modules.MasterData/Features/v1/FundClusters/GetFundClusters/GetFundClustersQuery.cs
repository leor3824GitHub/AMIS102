using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusters;

public sealed record GetFundClustersQuery(
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponseOfFundClusterDto>;

public sealed record PagedResponseOfFundClusterDto(
    ICollection<FundClusterDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record FundClusterDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);
