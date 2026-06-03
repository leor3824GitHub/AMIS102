using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.UpdateInspectionAcceptanceReport;

public static class UpdateInspectionAcceptanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithName($"Procurement.{nameof(UpdateInspectionAcceptanceReportCommand)}")
            .WithSummary("Update a Draft asset IAR")
            .Produces<InspectionAcceptanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.Update);

    private static async Task<IResult> Handle(Guid id, UpdateInspectionAcceptanceReportCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
