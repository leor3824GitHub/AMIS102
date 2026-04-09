using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public static class CreateDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateDisbursementVoucherCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/finance/disbursement-vouchers/{id}", new { id });
        })
        .WithName(nameof(CreateDisbursementVoucherCommand))
        .WithSummary("Create a new disbursement voucher")
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.Create);
}
