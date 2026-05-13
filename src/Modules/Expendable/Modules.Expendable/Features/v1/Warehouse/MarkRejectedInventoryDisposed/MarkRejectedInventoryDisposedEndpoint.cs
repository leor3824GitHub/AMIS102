using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryDisposed;

public static class MarkRejectedInventoryDisposedEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/rejected/{rejectedId:guid}/dispose", MarkDisposed)
            .WithName(nameof(MarkRejectedInventoryDisposedCommand))
            .WithSummary("Mark rejected inventory as disposed")
            .Produces<MarkRejectedInventoryDisposedResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Consume);

    private static async Task<IResult> MarkDisposed(
        Guid rejectedId,
        MarkRejectedInventoryDisposedCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { RejectedInventoryId = rejectedId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


