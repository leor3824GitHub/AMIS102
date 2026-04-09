using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.SearchBudgetUtilizationRecords;

public static class SearchBudgetUtilizationRecordsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchBudgetUtilizationRecordsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchBudgetUtilizationRecordsQuery))
        .WithSummary("Search budget utilization records")
        .RequirePermission(FinanceModuleConstants.Permissions.BudgetUtilizationRecords.View);
}
