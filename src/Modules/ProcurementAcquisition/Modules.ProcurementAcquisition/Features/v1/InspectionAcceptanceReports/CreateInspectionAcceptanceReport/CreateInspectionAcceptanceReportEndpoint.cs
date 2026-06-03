using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CreateInspectionAcceptanceReport;

public static class CreateInspectionAcceptanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName($"Procurement.{nameof(CreateInspectionAcceptanceReportCommand)}")
            .WithSummary("Create an asset inspection and acceptance report")
            .Produces<InspectionAcceptanceReportDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.Create);

    private static async Task<IResult> Handle(
        CreateInspectionAcceptanceReportCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/procurement/inspection-acceptance-reports/{result.Id}", result);
    }
}
