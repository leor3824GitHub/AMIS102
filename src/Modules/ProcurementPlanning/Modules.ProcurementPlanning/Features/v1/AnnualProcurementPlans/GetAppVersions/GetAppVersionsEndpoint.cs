using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;

public static class GetAppVersionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/versions/{chainId:guid}", Handle)
            .WithName(nameof(GetAppVersionsQuery))
            .WithSummary("Get all versions in an APP version chain")
            .Produces<IReadOnlyList<AnnualProcurementPlanSummaryDto>>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.View);

    private static async Task<IResult> Handle(Guid chainId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAppVersionsQuery(chainId), ct);
        return TypedResults.Ok(result);
    }
}
