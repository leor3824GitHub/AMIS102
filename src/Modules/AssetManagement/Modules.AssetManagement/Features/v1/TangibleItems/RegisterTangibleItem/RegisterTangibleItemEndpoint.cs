using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;

public static class RegisterTangibleItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", RegisterTangibleItem)
            .WithName(nameof(RegisterTangibleItemCommand))
            .WithSummary("Register a new tangible item into inventory")
            .Produces<TangibleItemDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleItems.Create);

    private static async Task<IResult> RegisterTangibleItem(
        RegisterTangibleItemCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/tangible-items/{result.Id}", result);
    }
}

