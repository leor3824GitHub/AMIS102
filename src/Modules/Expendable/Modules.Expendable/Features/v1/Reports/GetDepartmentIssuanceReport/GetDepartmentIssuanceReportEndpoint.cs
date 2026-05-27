using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Expendable.Contracts.Permissions;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;

public static class GetDepartmentIssuanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/department-issuance", GetReport)
            .WithName(nameof(GetDepartmentIssuanceReportQuery))
            .WithSummary("Aggregated issuance report grouped by department â€” for supply officer reporting")
            .Produces<PagedResponse<DepartmentIssuanceSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.ViewReports);

    private static async Task<IResult> GetReport(
        [AsParameters] GetDepartmentIssuanceReportQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

