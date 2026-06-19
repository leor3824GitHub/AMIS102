using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetStatusCounts;

public static class GetBudgetUtilizationRecordStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", async ([AsParameters] GetBudgetUtilizationRecordStatusCountsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName($"Finance.{nameof(GetBudgetUtilizationRecordStatusCountsQuery)}")
        .WithSummary("Get budget utilization record counts grouped by status")
        .RequirePermission(FinancePermissions.BudgetUtilizationRecords.View);
}
