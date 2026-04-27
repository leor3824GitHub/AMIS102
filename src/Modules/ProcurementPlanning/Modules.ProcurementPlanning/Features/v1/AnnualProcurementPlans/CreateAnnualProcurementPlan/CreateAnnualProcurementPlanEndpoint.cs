using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;

public static class CreateAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName(nameof(CreateAnnualProcurementPlanCommand))
            .WithSummary("Create a new Annual Procurement Plan (APP)")
            .Produces<AnnualProcurementPlanDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Create);

    private static async Task<IResult> Handle(
        CreateAnnualProcurementPlanCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Created($"/api/v1/procurement-planning/apps/{result.Id}", result);
    }
}
