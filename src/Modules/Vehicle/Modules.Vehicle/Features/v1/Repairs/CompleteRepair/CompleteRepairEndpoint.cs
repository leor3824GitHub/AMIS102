using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.CompleteRepair;

public static class CompleteRepairEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/complete", async (Guid id, CompleteRepairCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(cmd with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(CompleteRepairCommand))
        .WithSummary("Complete a repair — reactivates vehicle if no other active repairs")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.Update);
}
