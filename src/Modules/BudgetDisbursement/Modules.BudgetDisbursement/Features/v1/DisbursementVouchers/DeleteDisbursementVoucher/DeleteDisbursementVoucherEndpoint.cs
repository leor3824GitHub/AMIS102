using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.DeleteDisbursementVoucher;

public static class DeleteDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteDisbursementVoucherCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_DeleteDisbursementVoucher")
        .WithSummary("Delete a Draft disbursement voucher")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.Delete);
}
