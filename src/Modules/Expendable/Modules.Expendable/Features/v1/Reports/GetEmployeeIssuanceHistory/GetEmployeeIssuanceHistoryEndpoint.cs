using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Expendable.Contracts.Permissions;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;

public static class GetEmployeeIssuanceHistoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/employee-issuance/all", GetAllHistory)
            .WithName("Expendable_GetEmployeeIssuanceHistoryAll")
            .WithSummary("Full employee issuance history (unpaged) for reporting and export")
            .Produces<IReadOnlyList<EmployeeIssuanceDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.ViewReports);

        return endpoints.MapGet("/employee-issuance", GetHistory)
            .WithName(nameof(GetEmployeeIssuanceHistoryQuery))
            .WithSummary("Per-employee issuance history â€” supplies issued per request with quantities and values")
            .Produces<PagedResponse<EmployeeIssuanceDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.ViewReports);
    }

    private static async Task<IResult> GetHistory(
        [AsParameters] GetEmployeeIssuanceHistoryQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetAllHistory(
        string? employeeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetEmployeeIssuanceHistoryAllQuery(employeeId, from, to), cancellationToken);
        return TypedResults.Ok(result);
    }
}

