using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public static class CancelDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", async (Guid id, CancelDisbursementVoucherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CancelDisbursementVoucherCommand(id, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(CancelDisbursementVoucherCommand))
        .WithSummary("Cancel a disbursement voucher")
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.Cancel);

    public sealed record CancelDisbursementVoucherRequest(string Remarks);
}
