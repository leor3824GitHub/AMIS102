using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.SearchIncidentReports;

public static class SearchIncidentReportsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchIncidentReportsQuery))
            .WithSummary("Search incident reports")
            .Produces<PagedResponse<PropertyIncidentReportSummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        PropertyIncidentType? incidentType = null,
        PropertyIncidentStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchIncidentReportsQuery(
            keyword, incidentType, status, fromDate, toDate, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}

