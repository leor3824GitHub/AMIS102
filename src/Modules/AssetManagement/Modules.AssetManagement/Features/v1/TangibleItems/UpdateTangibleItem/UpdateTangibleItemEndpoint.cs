using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.UpdateTangibleItem;

public static class UpdateTangibleItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdateTangibleItem)
            .WithName(nameof(UpdateTangibleItemCommand))
            .WithSummary("Update an existing tangible item")
            .Produces<TangibleItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleItems.Update);

    private static async Task<IResult> UpdateTangibleItem(
        Guid id,
        UpdateTangibleItemCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
