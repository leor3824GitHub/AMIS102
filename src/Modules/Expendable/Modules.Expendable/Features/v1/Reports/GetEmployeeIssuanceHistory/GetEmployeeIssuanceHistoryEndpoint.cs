using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;

public static class GetEmployeeIssuanceHistoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/employee-issuance", GetHistory)
            .WithName(nameof(GetEmployeeIssuanceHistoryQuery))
            .WithSummary("Per-employee issuance history — supplies issued per request with quantities and values")
            .Produces<PagedResponse<EmployeeIssuanceDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> GetHistory(
        [AsParameters] GetEmployeeIssuanceHistoryQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

