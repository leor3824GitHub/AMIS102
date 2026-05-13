using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.RecordPhysicalCountEntry;

public static class RecordPhysicalCountEntryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{sessionId:guid}/entries/{entryId:guid}", async (
            Guid sessionId,
            Guid entryId,
            [FromBody] RecordPhysicalCountEntryRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RecordPhysicalCountEntryCommand(
                sessionId, entryId,
                request.Result, request.Condition,
                request.QuantityOnHand, request.Remarks,
                request.IsScanned, request.PhotoPath);

            var result = await mediator.Send(command, ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetManagement_RecordPhysicalCountEntry")
        .WithSummary("Record the physical count result for a single asset entry (manual or camera scan)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.Update);
}

public sealed record RecordPhysicalCountEntryRequest(
    AMIS.Modules.AssetManagement.Domain.PhysicalCountEntryResult Result,
    AMIS.Modules.AssetManagement.Domain.PhysicalCountCondition? Condition,
    int QuantityOnHand,
    string? Remarks,
    bool IsScanned,
    string? PhotoPath);

