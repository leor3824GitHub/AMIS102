using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusters;

public static class GetFundClustersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetList)
            .WithName(nameof(GetFundClustersQuery))
            .WithSummary("Get paginated list of fund clusters")
            .Produces<PagedResponseOfFundClusterDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.FundClusters.View);

    private static async Task<IResult> GetList(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundClustersQuery(keyword, pageNumber, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }
}
