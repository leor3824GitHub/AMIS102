using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.PayDisbursementVoucher;

public static class PayDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/pay", async (Guid id, PayDisbursementVoucherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new PayDisbursementVoucherCommand(id, request.PaidDate, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(PayDisbursementVoucherCommand))
        .WithSummary("Mark a disbursement voucher as paid")
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.Pay);

    public sealed record PayDisbursementVoucherRequest(DateOnly PaidDate, string? Remarks);
}

