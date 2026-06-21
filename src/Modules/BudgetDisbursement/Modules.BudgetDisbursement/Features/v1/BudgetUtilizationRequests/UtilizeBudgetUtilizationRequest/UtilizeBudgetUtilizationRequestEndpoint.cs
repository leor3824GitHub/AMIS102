using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.UtilizeBudgetUtilizationRequest;

public static class UtilizeBudgetUtilizationRequestEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/utilize", async (Guid id, UtilizeBudgetUtilizationRequestRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UtilizeBudgetUtilizationRequestCommand(id, request.DisbursementVoucherId, request.DisbursementVoucherNumber), ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_UtilizeBudgetUtilizationRequest")
        .WithSummary("Utilize a budget utilization record")
        .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.Utilize);

    public sealed record UtilizeBudgetUtilizationRequestRequest(Guid DisbursementVoucherId, string DisbursementVoucherNumber);
}
