using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;

public static class CreatePurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreatePurchaseRequest)
            .WithName(nameof(CreatePurchaseRequestCommand))
            .WithSummary("Create a new purchase request")
            .Produces<PurchaseRequestDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.Create);

    private static async Task<IResult> CreatePurchaseRequest(
        CreatePurchaseRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/procurement/purchase-requests/{result.Id}", result);
    }
}
