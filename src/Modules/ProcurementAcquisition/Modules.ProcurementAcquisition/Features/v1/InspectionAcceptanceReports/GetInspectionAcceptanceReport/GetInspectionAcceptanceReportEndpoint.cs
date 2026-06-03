using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetInspectionAcceptanceReport;

public static class GetInspectionAcceptanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName($"Procurement.{nameof(GetInspectionAcceptanceReportQuery)}")
            .WithSummary("Get asset IAR by ID")
            .Produces<InspectionAcceptanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInspectionAcceptanceReportQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
