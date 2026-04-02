using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.DeleteRepairRecord;

public static class DeleteRepairRecordEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteRepairRecordCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeleteRepairRecordCommand))
        .WithSummary("Soft-delete a repair record")
        .Produces(StatusCodes.Status204NoContent)
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.Delete);
}
