using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.CancelBudgetUtilizationRecord;

public static class CancelBudgetUtilizationRecordEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", async (Guid id, CancelBudgetUtilizationRecordRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CancelBudgetUtilizationRecordCommand(id, request.Remarks), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(CancelBudgetUtilizationRecordCommand))
        .WithSummary("Cancel a budget utilization record")
        .RequirePermission(FinanceModuleConstants.Permissions.BudgetUtilizationRecords.Cancel);

    public sealed record CancelBudgetUtilizationRecordRequest(string Remarks);
}
