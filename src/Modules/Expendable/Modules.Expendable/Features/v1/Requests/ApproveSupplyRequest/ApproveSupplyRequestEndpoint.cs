using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.ApproveSupplyRequest;

public static class ApproveSupplyRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/approve", ApproveRequest)
            .WithName(nameof(ApproveSupplyRequestCommand))
            .WithSummary("Approve supply request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Approve);

    private static async Task<IResult> ApproveRequest(
        Guid id,
        ApproveSupplyRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { Id = id };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


