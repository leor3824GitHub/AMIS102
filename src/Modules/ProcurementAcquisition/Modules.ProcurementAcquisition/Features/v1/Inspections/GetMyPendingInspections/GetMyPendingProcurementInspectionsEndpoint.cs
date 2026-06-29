using AMIS.Framework.Shared.Inspections;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Inspections;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Inspections.GetMyPendingInspections;

public static class GetMyPendingProcurementInspectionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/pending-for-me", GetMyPendingInspections)
            .WithName("Procurement_GetMyPendingInspections")
            .WithSummary("List the Job Orders and IARs awaiting the current user's inspection")
            .Produces<IReadOnlyList<PendingInspectionItem>>(StatusCodes.Status200OK)
            .RequireAuthorization();

    private static async Task<IResult> GetMyPendingInspections(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyPendingProcurementInspectionsQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
