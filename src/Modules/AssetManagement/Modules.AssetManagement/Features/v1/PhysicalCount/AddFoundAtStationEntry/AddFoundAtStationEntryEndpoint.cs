using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.AddFoundAtStationEntry;

public static class AddFoundAtStationEntryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{sessionId:guid}/found-at-station", async (
            Guid sessionId,
            [FromBody] AddFoundAtStationEntryBody body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new AddFoundAtStationEntryCommand(
                sessionId,
                body.PropertyNumber,
                body.Description,
                body.UnitCost,
                body.Condition,
                body.Remarks,
                body.PhotoPath);

            var result = await mediator.Send(command, ct);
            return TypedResults.Created(
                $"/api/v1/asset-management/physical-count/{sessionId}/entries/{result.EntryId}", result);
        })
        .WithName("AssetManagement_AddFoundAtStationEntry")
        .WithSummary("Add a 'Found at Station' entry for an asset not on the pre-generated checklist")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.Update);
}

public sealed record AddFoundAtStationEntryBody(
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    FSH.Modules.AssetManagement.Domain.PhysicalCountCondition Condition,
    string? Remarks,
    string? PhotoPath);
