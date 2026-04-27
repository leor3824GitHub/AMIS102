using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;

public static class ReturnAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/return", Handle)
            .WithName(nameof(ReturnAppCommand))
            .WithSummary("Return a submitted APP for revision")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Return);

    private static async Task<IResult> Handle(
        Guid id, ReturnAppCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Ok(result);
    }
}
