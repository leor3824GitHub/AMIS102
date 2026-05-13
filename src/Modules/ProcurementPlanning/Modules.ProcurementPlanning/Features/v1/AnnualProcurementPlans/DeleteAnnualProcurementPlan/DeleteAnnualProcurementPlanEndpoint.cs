using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.DeleteAnnualProcurementPlan;

public static class DeleteAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Handle)
            .WithName(nameof(DeleteAnnualProcurementPlanCommand))
            .WithSummary("Delete a Draft APP and revert any consolidated PPMPs")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Delete);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeleteAnnualProcurementPlanCommand(id), ct);
        return TypedResults.NoContent();
    }
}

