using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Lookups;

public static class VehicleLookupEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/vehicles", SearchVehicles)
            .WithName(nameof(SearchVehicleReferencesQuery))
            .WithSummary("Search vehicle references")
            .Produces<PagedResponse<VehicleReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(VehicleModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/vehicles/{id:guid}", GetVehicleById)
            .WithName(nameof(GetVehicleReferenceByIdQuery))
            .WithSummary("Get vehicle reference by id")
            .Produces<VehicleReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(VehicleModuleConstants.Permissions.Lookup.View);
    }

    private static async Task<IResult> SearchVehicles(
        [AsParameters] SearchVehicleReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetVehicleById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVehicleReferenceByIdQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
