using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.SearchBudgetUtilizationRequests;

public static class SearchBudgetUtilizationRequestsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchBudgetUtilizationRequestsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName("BudgetDisbursement_SearchBudgetUtilizationRequests")
        .WithSummary("Search budget utilization records")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.View);
}

