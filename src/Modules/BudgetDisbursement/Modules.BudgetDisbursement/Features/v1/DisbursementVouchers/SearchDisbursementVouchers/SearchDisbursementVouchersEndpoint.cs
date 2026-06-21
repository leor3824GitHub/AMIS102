using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;

public static class SearchDisbursementVouchersEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchDisbursementVouchersQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName("BudgetDisbursement_SearchDisbursementVouchers")
        .WithSummary("Search disbursement vouchers")
        .RequirePermission(BudgetDisbursementPermissions.DisbursementVouchers.View);
}

