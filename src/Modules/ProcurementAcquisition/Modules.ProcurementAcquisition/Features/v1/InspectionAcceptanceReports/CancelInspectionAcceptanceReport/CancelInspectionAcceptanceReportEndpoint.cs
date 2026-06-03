using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CancelInspectionAcceptanceReport;

public static class CancelInspectionAcceptanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", Handle)
            .WithName($"Procurement.{nameof(CancelInspectionAcceptanceReportCommand)}")
            .WithSummary("Cancel an IAR before it is accepted")
            .Produces<InspectionAcceptanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.Cancel);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelInspectionAcceptanceReportCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
