using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.UpdateFundCluster;

public static class UpdateFundClusterEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id}", Update)
            .WithName(nameof(UpdateFundClusterCommand))
            .WithSummary("Update fund cluster")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.FundClusters.Update);

    private static async Task<IResult> Update(
        Guid id,
        UpdateFundClusterCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.NoContent();
    }
}
