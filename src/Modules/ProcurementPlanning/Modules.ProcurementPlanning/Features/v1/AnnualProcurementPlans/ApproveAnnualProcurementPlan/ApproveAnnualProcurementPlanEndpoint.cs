using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public static class ApproveAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/approve", Handle)
            .WithName(nameof(ApproveAppCommand))
            .WithSummary("Approve a submitted APP")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Approve);

    private static async Task<IResult> Handle(
        Guid id, ApproveAppCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Ok(result);
    }
}

