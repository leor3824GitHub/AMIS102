using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SearchPpmps;

public static class SearchPpmpsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchPpmpsQuery))
            .WithSummary("Search PPMPs")
            .Produces<PagedResponse<PpmpSummaryDto>>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchPpmpsQuery query, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }
}
