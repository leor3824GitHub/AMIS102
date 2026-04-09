using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetBudgetUtilizationRecordById;

public static class GetBudgetUtilizationRecordByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetBudgetUtilizationRecordByIdQuery(id), ct)))
        .WithName(nameof(GetBudgetUtilizationRecordByIdQuery))
        .WithSummary("Get budget utilization record by ID")
        .RequirePermission(FinanceModuleConstants.Permissions.BudgetUtilizationRecords.View);
}
