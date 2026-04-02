using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.UpdateRepairRecord;

public static class UpdateRepairRecordEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateRepairRecordCommand cmd, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(cmd with { Id = id }, ct)))
        .WithName(nameof(UpdateRepairRecordCommand))
        .WithSummary("Update a repair record")
        .Produces<RepairRecordDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.Update);
}
