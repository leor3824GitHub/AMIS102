using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;

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
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.Approve);
}
