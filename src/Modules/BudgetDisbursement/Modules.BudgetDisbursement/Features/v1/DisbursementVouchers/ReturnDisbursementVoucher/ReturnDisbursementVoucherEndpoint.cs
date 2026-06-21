using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.ReturnDisbursementVoucher;

public static class ReturnDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/return", async (Guid id, ReturnDisbursementVoucherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ReturnDisbursementVoucherCommand(id, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(ReturnDisbursementVoucherCommand))
        .WithSummary("Return a disbursement voucher")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.Return);

    public sealed record ReturnDisbursementVoucherRequest(string Remarks);
}
