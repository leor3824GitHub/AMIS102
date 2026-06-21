using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.Settings.GetBudgetDisbursementSettings;

public static class GetBudgetDisbursementSettingsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetBudgetDisbursementSettingsQuery(), ct)))
        .WithName("BudgetDisbursement_GetSettings")
        .WithSummary("Get Budget Disbursement module admin settings")
        .RequirePermission(BudgetDisbursementPermissions.Settings.View);
}
