using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SubmitJobOrder;

public static class SubmitJobOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit", SubmitJobOrder)
            .WithName($"Procurement.{nameof(SubmitJobOrderCommand)}")
            .WithSummary("Submit a draft job order for funds-available certification")
            .Produces<JobOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.JobOrders.Submit);

    private static async Task<IResult> SubmitJobOrder(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitJobOrderCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
