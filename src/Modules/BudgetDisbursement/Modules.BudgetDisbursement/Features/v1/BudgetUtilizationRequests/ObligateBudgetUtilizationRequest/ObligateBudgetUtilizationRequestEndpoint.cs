using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.ObligateBudgetUtilizationRequest;

public static class ObligateBudgetUtilizationRequestEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/obligate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ObligateBudgetUtilizationRequestCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(ObligateBudgetUtilizationRequestCommand))
        .WithSummary("Obligate a budget utilization record")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.Obligate);
}

