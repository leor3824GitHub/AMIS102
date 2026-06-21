using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public static class CancelDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", async (Guid id, CancelDisbursementVoucherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CancelDisbursementVoucherCommand(id, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_CancelDisbursementVoucher")
        .WithSummary("Cancel a disbursement voucher")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.Cancel);

    public sealed record CancelDisbursementVoucherRequest(string Remarks);
}

