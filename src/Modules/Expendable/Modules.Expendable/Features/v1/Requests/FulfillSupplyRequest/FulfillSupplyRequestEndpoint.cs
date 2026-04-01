using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Requests.FulfillSupplyRequest;

public static class FulfillSupplyRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/fulfill", FulfillRequest)
            .WithName(nameof(FulfillSupplyRequestCommand))
            .WithSummary("Fulfill an approved supply request — issues from warehouse and records per-employee receipt")
            .Produces<FulfillSupplyRequestResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.Fulfill);

    private static async Task<IResult> FulfillRequest(
        Guid id,
        FulfillSupplyRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { SupplyRequestId = id };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}
