using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.DeleteBudgetUtilizationRequest;

public static class DeleteBudgetUtilizationRequestEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteBudgetUtilizationRequestCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_DeleteBudgetUtilizationRequest")
        .WithSummary("Delete a Draft budget utilization request")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.Delete);
}
