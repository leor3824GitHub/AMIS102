using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;

public static class ConsolidatePpmpsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/consolidate", Handle)
            .WithName(nameof(ConsolidatePpmpsCommand))
            .WithSummary("Consolidate approved PPMPs into an APP")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.Consolidate);

    private static async Task<IResult> Handle(
        Guid id, ConsolidatePpmpsCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { AppId = id }, ct);
        return TypedResults.Ok(result);
    }
}

