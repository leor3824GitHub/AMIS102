using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Reclassification.GetReclassificationHistory;

public static class GetReclassificationHistoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetReclassificationHistory)
            .WithName(nameof(GetReclassificationHistoryQuery))
            .WithSummary("Get paginated history of property reclassification runs")
            .Produces<PagedReclassificationHistoryResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reclassification.View);

    private static async Task<IResult> GetReclassificationHistory(
        [AsParameters] GetReclassificationHistoryQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

