using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;

public static class GetDisbursementVoucherByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetDisbursementVoucherByIdQuery(id), ct)))
        .WithName(nameof(GetDisbursementVoucherByIdQuery))
        .WithSummary("Get disbursement voucher by ID")
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.View);
}
