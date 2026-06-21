using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CancelBudgetUtilizationRequest;

public static class CancelBudgetUtilizationRequestEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", async (Guid id, CancelBudgetUtilizationRequestRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CancelBudgetUtilizationRequestCommand(id, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_CancelBudgetUtilizationRequest")
        .WithSummary("Cancel a budget utilization record")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.Cancel);

    public sealed record CancelBudgetUtilizationRequestRequest(string Remarks);
}

