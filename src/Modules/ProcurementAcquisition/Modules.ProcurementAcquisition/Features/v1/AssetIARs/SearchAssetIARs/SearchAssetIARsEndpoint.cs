using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SearchAssetIARs;

public static class SearchAssetIARsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName($"Procurement.{nameof(SearchAssetIARsQuery)}")
            .WithSummary("Search asset inspection and acceptance reports")
            .Produces<PagedResponse<AssetIARSummaryDto>>()
            .RequirePermission(ProcurementPermissions.AssetIARs.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAssetIARsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
