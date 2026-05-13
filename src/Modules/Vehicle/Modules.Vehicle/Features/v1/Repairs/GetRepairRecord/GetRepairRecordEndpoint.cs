using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.GetRepairRecord;

public static class GetRepairRecordEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetRepairRecordQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName(nameof(GetRepairRecordQuery))
        .WithSummary("Get a repair record by ID")
        .Produces<RepairRecordDto>()
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.View);
}

