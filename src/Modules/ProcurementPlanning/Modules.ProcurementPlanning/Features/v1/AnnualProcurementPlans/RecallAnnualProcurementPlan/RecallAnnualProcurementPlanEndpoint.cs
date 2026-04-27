using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RecallAnnualProcurementPlan;

public static class RecallAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/recall", Handle)
            .WithName(nameof(RecallAppCommand))
            .WithSummary("Recall a submitted APP back to draft")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Publish);

    private static async Task<IResult> Handle(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RecallAppCommand(id), ct);
        return TypedResults.Ok(result);
    }
}
