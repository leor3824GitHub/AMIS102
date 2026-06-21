using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CreateBudgetUtilizationRequest;

public static class CreateBudgetUtilizationRequestEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateBudgetUtilizationRequestCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/budget-disbursement/budget-utilization-requests/{id}", new { id });
        })
        .WithName("BudgetDisbursement_CreateBudgetUtilizationRequest")
        .WithSummary("Create a new budget utilization record")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.Create);
}

