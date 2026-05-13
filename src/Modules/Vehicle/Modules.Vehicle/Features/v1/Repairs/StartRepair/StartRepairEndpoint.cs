using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.StartRepair;

public static class StartRepairEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/start", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new StartRepairCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(StartRepairCommand))
        .WithSummary("Start a repair — moves vehicle to UnderRepair status")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.Update);
}

