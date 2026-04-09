using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetPurchaseRequest;

public static class GetPurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetPurchaseRequest)
            .WithName(nameof(GetPurchaseRequestQuery))
            .WithSummary("Get a purchase request by ID")
            .Produces<PurchaseRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.View);

    private static async Task<IResult> GetPurchaseRequest(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPurchaseRequestQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
