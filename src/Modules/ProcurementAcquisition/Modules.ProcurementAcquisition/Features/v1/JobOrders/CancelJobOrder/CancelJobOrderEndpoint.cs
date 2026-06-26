using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CancelJobOrder;

public static class CancelJobOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatch("/{id:guid}/cancel", CancelJobOrder)
            .WithName($"Procurement.{nameof(CancelJobOrderCommand)}")
            .WithSummary("Cancel a job order")
            .Produces<JobOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.JobOrders.Cancel);

    private static async Task<IResult> CancelJobOrder(
        Guid id,
        CancelJobOrderRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelJobOrderCommand(id, request.Reason), cancellationToken);
        return TypedResults.Ok(result);
    }
}

public sealed record CancelJobOrderRequest(string? Reason = null);
