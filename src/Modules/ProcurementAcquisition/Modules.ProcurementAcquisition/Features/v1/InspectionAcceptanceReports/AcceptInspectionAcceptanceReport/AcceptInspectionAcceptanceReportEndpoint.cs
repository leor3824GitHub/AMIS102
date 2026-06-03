using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AcceptInspectionAcceptanceReport;

public static class AcceptInspectionAcceptanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/accept", Handle)
            .WithName($"Procurement.{nameof(AcceptInspectionAcceptanceReportCommand)}")
            .WithSummary("Accept an asset IAR â€” triggers TangibleItem creation in AssetManagement")
            .Produces<InspectionAcceptanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.Accept);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptInspectionAcceptanceReportCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
