using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;

public static class SearchDisbursementVouchersEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchDisbursementVouchersQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchDisbursementVouchersQuery))
        .WithSummary("Search disbursement vouchers")
        .RequirePermission(FinanceModuleConstants.Permissions.DisbursementVouchers.View);
}

