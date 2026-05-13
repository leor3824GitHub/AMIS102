using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public static class RemoveSupplyRequestItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{requestId:guid}/items/{productId:guid}", RemoveItem)
            .WithName(nameof(RemoveSupplyRequestItemCommand))
            .WithSummary("Remove item from supply request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Update);

    private static async Task<IResult> RemoveItem(
        Guid requestId,
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RemoveSupplyRequestItemCommand(requestId, productId);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


