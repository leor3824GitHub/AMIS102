using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ExportAppXlsx;

public static class ExportAppXlsxEndpoint
{
    internal const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/xlsx", Handle)
            .WithName("ProcurementPlanning_ExportAnnualProcurementPlanXlsx")
            .WithSummary("Export an APP to Excel (.xlsx) in the GPPB APP form layout")
            .Produces(StatusCodes.Status200OK, contentType: ContentType)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var bytes = await mediator.Send(new ExportAppXlsxQuery(id), ct);
        return TypedResults.File(bytes, ContentType, "AnnualProcurementPlan.xlsx");
    }
}
