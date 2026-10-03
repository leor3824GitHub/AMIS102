using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SubmitAnnualProcurementPlan;

public static class SubmitAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit", Handle)
            .WithName(nameof(SubmitAnnualProcurementPlanCommand))
            .WithSummary("Submit an APP for approval (locks it read-only)")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.Submit);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitAnnualProcurementPlanCommand(id), ct);
        return TypedResults.Ok(result);
    }
}

