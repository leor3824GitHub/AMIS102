using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;

public static class GetDisbursementVoucherByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetDisbursementVoucherByIdQuery(id), ct)))
        .WithName("BudgetDisbursement_GetDisbursementVoucherById")
        .WithSummary("Get disbursement voucher by ID")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.View);
}

