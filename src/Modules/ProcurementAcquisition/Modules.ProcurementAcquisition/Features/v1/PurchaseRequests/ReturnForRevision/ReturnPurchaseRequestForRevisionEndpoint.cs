using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ReturnForRevision;

public static class ReturnPurchaseRequestForRevisionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/return-for-revision", ReturnForRevision)
            .WithName($"Procurement.{nameof(ReturnPurchaseRequestForRevisionCommand)}")
            .WithSummary("Return a submitted PR to the requester for revision with a reason")
            .Produces<PurchaseRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.PurchaseRequests.ReturnForRevision);

    private static async Task<IResult> ReturnForRevision(
        Guid id,
        ReturnPurchaseRequestForRevisionCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
