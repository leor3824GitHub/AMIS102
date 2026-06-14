using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Reports.GetPropertyCard;

public static class GetPropertyCardEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/property-card/{propertyNo}", Handle)
            .WithModuleName<GetPropertyCardQuery>()
            .WithSummary("Generate the Property Card (COA chronological movement history) for an asset")
            .Produces<PropertyCardDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(string propertyNo, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPropertyCardQuery(propertyNo), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
