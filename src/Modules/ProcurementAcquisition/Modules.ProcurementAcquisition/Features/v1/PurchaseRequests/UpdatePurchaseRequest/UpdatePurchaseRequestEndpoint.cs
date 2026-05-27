using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;

public static class UpdatePurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdatePurchaseRequest)
            .WithName($"Procurement.{nameof(UpdatePurchaseRequestCommand)}")
            .WithSummary("Update a draft purchase request")
            .Produces<PurchaseRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.PurchaseRequests.Update);

    private static async Task<IResult> UpdatePurchaseRequest(
        Guid id,
        UpdatePurchaseRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}

