using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ExportPpmpXlsx;

public static class ExportPpmpXlsxEndpoint
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/xlsx", Handle)
            .WithName("ProcurementPlanning_ExportPpmpXlsx")
            .WithSummary("Export a PPMP to Excel (.xlsx) in the GPPB PPMP form layout")
            .Produces(StatusCodes.Status200OK, contentType: ContentType)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPlanningPermissions.Ppmps.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var bytes = await mediator.Send(new ExportPpmpXlsxQuery(id), ct);
        return TypedResults.File(bytes, ContentType, "PPMP.xlsx");
    }
}
