using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItemById;

public static class GetTangibleItemByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetTangibleItemById)
            .WithName(nameof(GetTangibleItemByIdQuery))
            .WithSummary("Get tangible item by ID")
            .Produces<TangibleItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleItems.View);

    private static async Task<IResult> GetTangibleItemById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTangibleItemByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

