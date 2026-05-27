using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportById;

public static class GetUnserviceablePropertyReportByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetById)
            .WithName(nameof(GetUnserviceablePropertyReportByIdQuery))
            .WithSummary("Get an Inspection and Inventory Report of Unserviceable Semi-Expendable Property (IIRUSP) by ID")
            .Produces<UnserviceablePropertyReportDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementPermissions.UnserviceablePropertyReports.View);

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUnserviceablePropertyReportByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

