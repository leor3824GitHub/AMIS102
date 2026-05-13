using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;

public static class CreateBudgetUtilizationRecordEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateBudgetUtilizationRecordCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/finance/budget-utilization-records/{id}", new { id });
        })
        .WithName(nameof(CreateBudgetUtilizationRecordCommand))
        .WithSummary("Create a new budget utilization record")
        .RequirePermission(FinanceModuleConstants.Permissions.BudgetUtilizationRecords.Create);
}

