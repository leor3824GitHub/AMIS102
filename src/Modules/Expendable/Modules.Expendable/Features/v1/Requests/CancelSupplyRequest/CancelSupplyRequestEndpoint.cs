using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Requests.CancelSupplyRequest;

public static class CancelSupplyRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/cancel", CancelRequest)
            .WithName(nameof(CancelSupplyRequestCommand))
            .WithSummary("Cancel supply request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Cancel);

    private static async Task<IResult> CancelRequest(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelSupplyRequestCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
