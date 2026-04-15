using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

public static class CreatePropertyIncidentReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateReport)
            .WithName(nameof(CreatePropertyIncidentReportCommand))
            .WithSummary("Create a Report of Lost/Stolen/Damaged/Destroyed Semi-Expendable Property (RLSDDSP)")
            .Produces<CreatePropertyIncidentReportResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.PropertyIncidentReports.Create);

    private static async Task<IResult> CreateReport(
        CreatePropertyIncidentReportCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/property-incident-reports/{result.ReportId}", result);
    }
}
