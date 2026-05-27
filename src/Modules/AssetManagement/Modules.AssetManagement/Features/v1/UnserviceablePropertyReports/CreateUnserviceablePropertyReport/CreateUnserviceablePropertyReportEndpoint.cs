using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

public static class CreateUnserviceablePropertyReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateReport)
            .WithName(nameof(CreateUnserviceablePropertyReportCommand))
            .WithSummary("Create an Inspection and Inventory Report of Unserviceable Semi-Expendable Property (IIRUSP)")
            .Produces<CreateUnserviceablePropertyReportResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementPermissions.UnserviceablePropertyReports.Create);

    private static async Task<IResult> CreateReport(
        CreateUnserviceablePropertyReportCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/unserviceable-property-reports/{result.ReportId}", result);
    }
}

