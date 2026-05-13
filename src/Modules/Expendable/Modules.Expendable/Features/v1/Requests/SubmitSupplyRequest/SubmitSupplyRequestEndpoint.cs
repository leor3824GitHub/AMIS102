using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.SubmitSupplyRequest;

public static class SubmitSupplyRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/submit", SubmitRequest)
            .WithName(nameof(SubmitSupplyRequestCommand))
            .WithSummary("Submit supply request for approval")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Update);

    private static async Task<IResult> SubmitRequest(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSupplyRequestCommand(id);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


