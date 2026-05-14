using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.SearchIssuanceReports;

public static class SearchIssuanceReportsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<SearchIssuanceReportsQuery>()
            .WithSummary("Search issuance reports")
            .Produces<PagedResponse<PropertyIssuanceReportSummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        IssuanceReportType? reportType = null,
        IssuanceReportStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchIssuanceReportsQuery(
            keyword, reportType, status, fromDate, toDate, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}

