using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;

public static class ApproveDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/approve", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ApproveDisbursementVoucherCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(ApproveDisbursementVoucherCommand))
        .WithSummary("Approve a disbursement voucher")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.Approve);
}

