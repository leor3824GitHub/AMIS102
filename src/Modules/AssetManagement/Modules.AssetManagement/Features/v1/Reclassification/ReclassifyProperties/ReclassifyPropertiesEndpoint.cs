using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;

public static class ReclassifyPropertiesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", ReclassifyProperties)
            .WithName(nameof(ReclassifyPropertiesCommand))
            .WithSummary("Reclassify all semi-expendable properties against the active threshold policy")
            .Produces<ReclassifyPropertiesResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reclassification.Create);

    private static async Task<IResult> ReclassifyProperties(
        ReclassifyPropertiesCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/reclassification/{result.RecordId}", result);
    }
}

