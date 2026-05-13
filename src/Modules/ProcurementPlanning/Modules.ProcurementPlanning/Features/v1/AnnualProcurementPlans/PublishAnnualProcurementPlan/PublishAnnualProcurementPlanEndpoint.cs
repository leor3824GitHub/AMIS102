using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;

public static class PublishAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/publish", Handle)
            .WithName(nameof(PublishAnnualProcurementPlanCommand))
            .WithSummary("Publish an APP (lock and make official)")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Publish);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishAnnualProcurementPlanCommand(id), ct);
        return TypedResults.Ok(result);
    }
}

