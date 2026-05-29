using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetStatusCounts;

public static class GetPurchaseRequestStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", GetStatusCounts)
            .WithName($"Procurement.{nameof(GetPurchaseRequestStatusCountsQuery)}")
            .WithSummary("Get purchase request counts grouped by status")
            .Produces<IReadOnlyList<PurchaseRequestStatusCountDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.PurchaseRequests.View);

    private static async Task<IResult> GetStatusCounts(
        [AsParameters] GetPurchaseRequestStatusCountsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
