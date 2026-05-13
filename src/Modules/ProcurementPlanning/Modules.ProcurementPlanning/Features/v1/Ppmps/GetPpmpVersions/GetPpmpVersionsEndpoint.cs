using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmpVersions;

public static class GetPpmpVersionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/versions/{chainId:guid}", Handle)
            .WithName(nameof(GetPpmpVersionsQuery))
            .WithSummary("Get all versions in a PPMP version chain")
            .Produces<IReadOnlyList<PpmpSummaryDto>>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.View);

    private static async Task<IResult> Handle(Guid chainId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPpmpVersionsQuery(chainId), ct);
        return TypedResults.Ok(result);
    }
}

