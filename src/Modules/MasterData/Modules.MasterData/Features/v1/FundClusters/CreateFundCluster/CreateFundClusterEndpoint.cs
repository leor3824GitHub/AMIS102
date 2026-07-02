using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.CreateFundCluster;

public static class CreateFundClusterEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Create)
            .WithName(nameof(CreateFundClusterCommand))
            .WithSummary("Create fund cluster")
            .Produces<FundClusterDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataPermissions.FundClusters.Create);

    private static async Task<IResult> Create(
        CreateFundClusterCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/fund-clusters/{result.Id}", result);
    }
}
