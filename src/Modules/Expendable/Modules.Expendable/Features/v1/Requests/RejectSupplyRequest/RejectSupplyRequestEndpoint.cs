using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.RejectSupplyRequest;

public static class RejectSupplyRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/reject", RejectRequest)
            .WithName(nameof(RejectSupplyRequestCommand))
            .WithSummary("Reject supply request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Reject);

    private static async Task<IResult> RejectRequest(
        Guid id,
        RejectSupplyRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { Id = id };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


