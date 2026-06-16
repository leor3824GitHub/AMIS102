using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Domain.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocations;

public static class GetLocationsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<GetLocationsQuery>()
            .WithSummary("Get paginated locations for asset accountability and placement")
            .Produces<PagedLocationsResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetRegisterPermissions.Locations.View);

    private static async Task<IResult> Handle(
        string? keyword,
        LocationType? type,
        Guid? parentLocationId,
        int pageNumber,
        int pageSize,
        IMediator mediator,
        CancellationToken ct) =>
        TypedResults.Ok(await mediator.Send(
            new GetLocationsQuery(keyword, type, parentLocationId, pageNumber, pageSize), ct));
}
