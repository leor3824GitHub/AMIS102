using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.Settings.UpdateBudgetDisbursementSettings;

public static class UpdateBudgetDisbursementSettingsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/", async (UpdateBudgetDisbursementSettingsCommand command, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return TypedResults.NoContent();
        })
        .WithName("BudgetDisbursement_UpdateSettings")
        .WithSummary("Update Budget Disbursement module admin settings")
        .RequirePermission(BudgetDisbursementPermissions.Settings.Update);
}
