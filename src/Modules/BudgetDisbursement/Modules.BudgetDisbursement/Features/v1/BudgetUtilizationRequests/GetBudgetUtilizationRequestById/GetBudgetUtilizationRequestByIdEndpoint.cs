using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetBudgetUtilizationRequestById;

public static class GetBudgetUtilizationRequestByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetBudgetUtilizationRequestByIdQuery(id), ct)))
        .WithName("BudgetDisbursement_GetBudgetUtilizationRequestById")
        .WithSummary("Get budget utilization record by ID")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.View);
}

