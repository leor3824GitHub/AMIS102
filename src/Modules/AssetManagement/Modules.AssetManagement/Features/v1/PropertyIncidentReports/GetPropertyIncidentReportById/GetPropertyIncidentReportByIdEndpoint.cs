using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportById;

public static class GetPropertyIncidentReportByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetById)
            .WithName(nameof(GetPropertyIncidentReportByIdQuery))
            .WithSummary("Get a Property Incident Report (RLSDDSP) by ID")
            .Produces<PropertyIncidentReportDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.PropertyIncidentReports.View);

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPropertyIncidentReportByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

