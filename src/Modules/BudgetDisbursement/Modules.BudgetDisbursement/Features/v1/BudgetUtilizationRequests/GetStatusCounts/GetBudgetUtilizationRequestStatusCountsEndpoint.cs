using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetStatusCounts;

public static class GetBudgetUtilizationRequestStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", async ([AsParameters] GetBudgetUtilizationRequestStatusCountsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName($"BudgetDisbursement.{nameof(GetBudgetUtilizationRequestStatusCountsQuery)}")
        .WithSummary("Get budget utilization record counts grouped by status")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.View);
}
