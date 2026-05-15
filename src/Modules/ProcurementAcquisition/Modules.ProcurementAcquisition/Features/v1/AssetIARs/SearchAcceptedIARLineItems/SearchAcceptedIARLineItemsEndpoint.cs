using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SearchAcceptedIARLineItems;

public static class SearchAcceptedIARLineItemsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/accepted-line-items", Handle)
            .WithName($"Procurement.{nameof(SearchAcceptedIARLineItemsQuery)}")
            .WithSummary("Search accepted IAR line items (for Receiving Report pre-fill)")
            .Produces<PagedResponse<AcceptedIARLineItemDto>>()
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAcceptedIARLineItemsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
