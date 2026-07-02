using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusterById;

public static class GetFundClusterByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id}", GetById)
            .WithName(nameof(GetFundClusterByIdQuery))
            .WithSummary("Get fund cluster by ID")
            .Produces<FundClusterDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.FundClusters.View);

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFundClusterByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
