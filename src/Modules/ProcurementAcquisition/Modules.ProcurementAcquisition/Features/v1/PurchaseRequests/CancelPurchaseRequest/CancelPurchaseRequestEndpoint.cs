using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CancelPurchaseRequest;

public static class CancelPurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", CancelPurchaseRequest)
            .WithName(nameof(CancelPurchaseRequestCommand))
            .WithSummary("Cancel a purchase request")
            .Produces<PurchaseRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.Cancel);

    private static async Task<IResult> CancelPurchaseRequest(
        Guid id,
        CancelPurchaseRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
