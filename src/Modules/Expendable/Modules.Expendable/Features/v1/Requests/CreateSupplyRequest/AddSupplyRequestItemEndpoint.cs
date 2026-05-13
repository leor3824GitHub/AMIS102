using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public static class AddSupplyRequestItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{requestId:guid}/items", AddItem)
            .WithName(nameof(AddSupplyRequestItemCommand))
            .WithSummary("Add item to supply request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Update);

    private static async Task<IResult> AddItem(
        Guid requestId,
        AddSupplyRequestItemCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { RequestId = requestId };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


