using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Finance.Contracts.Permissions;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.UtilizeBudgetUtilizationRecord;

public static class UtilizeBudgetUtilizationRecordEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/utilize", async (Guid id, UtilizeBudgetUtilizationRecordRequest request, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UtilizeBudgetUtilizationRecordCommand(id, request.DisbursementVoucherId, request.DisbursementVoucherNumber), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(UtilizeBudgetUtilizationRecordCommand))
        .WithSummary("Utilize a budget utilization record")
        .RequirePermission(FinancePermissions.BudgetUtilizationRecords.Utilize);

    public sealed record UtilizeBudgetUtilizationRecordRequest(Guid DisbursementVoucherId, string DisbursementVoucherNumber);
}
