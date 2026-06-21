using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetStatusCounts;

public static class GetDisbursementVoucherStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", async ([AsParameters] GetDisbursementVoucherStatusCountsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName("BudgetDisbursement_GetDisbursementVoucherStatusCounts")
        .WithSummary("Get disbursement voucher counts grouped by status")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.View);
}
