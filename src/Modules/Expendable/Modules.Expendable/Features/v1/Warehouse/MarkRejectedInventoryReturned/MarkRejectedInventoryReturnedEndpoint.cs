using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryReturned;

public static class MarkRejectedInventoryReturnedEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/rejected/{rejectedId:guid}/return", MarkReturned)
            .WithName(nameof(MarkRejectedInventoryReturnedCommand))
            .WithSummary("Mark rejected inventory as returned")
            .Produces<MarkRejectedInventoryReturnedResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Receive);

    private static async Task<IResult> MarkReturned(
        Guid rejectedId,
        MarkRejectedInventoryReturnedCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { RejectedInventoryId = rejectedId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


