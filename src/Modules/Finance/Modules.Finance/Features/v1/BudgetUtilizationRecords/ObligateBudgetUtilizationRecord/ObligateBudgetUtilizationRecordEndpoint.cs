using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.ObligateBudgetUtilizationRecord;

public static class ObligateBudgetUtilizationRecordEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/obligate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ObligateBudgetUtilizationRecordCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(ObligateBudgetUtilizationRecordCommand))
        .WithSummary("Obligate a budget utilization record")
        .RequirePermission(FinanceModuleConstants.Permissions.BudgetUtilizationRecords.Obligate);
}

