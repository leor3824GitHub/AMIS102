using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchAcceptedIARLineItems;

public static class SearchAcceptedIARLineItemsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/accepted-line-items", Handle)
            .WithName($"Procurement.{nameof(SearchAcceptedIARLineItemsQuery)}")
            .WithSummary("Search accepted IAR line items (for Receiving Report pre-fill)")
            .Produces<PagedResponse<AcceptedIARLineItemDto>>()
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAcceptedIARLineItemsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
