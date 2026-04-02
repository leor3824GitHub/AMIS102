using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.SearchVehicles;

public static class SearchVehiclesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchVehiclesQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchVehiclesQuery))
        .WithSummary("Search and filter vehicles")
        .Produces<PagedResponse<VehicleDto>>()
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.View);
}
