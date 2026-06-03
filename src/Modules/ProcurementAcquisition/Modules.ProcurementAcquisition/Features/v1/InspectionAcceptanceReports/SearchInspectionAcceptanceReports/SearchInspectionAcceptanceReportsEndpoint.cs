using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchInspectionAcceptanceReports;

public static class SearchInspectionAcceptanceReportsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName($"Procurement.{nameof(SearchInspectionAcceptanceReportsQuery)}")
            .WithSummary("Search asset inspection and acceptance reports")
            .Produces<PagedResponse<InspectionAcceptanceReportSummaryDto>>()
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchInspectionAcceptanceReportsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
