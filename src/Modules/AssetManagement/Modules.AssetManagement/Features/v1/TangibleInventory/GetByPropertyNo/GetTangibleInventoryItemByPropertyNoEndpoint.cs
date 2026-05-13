using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.GetByPropertyNo;

public static class GetTangibleInventoryItemByPropertyNoEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/by-property-no/{propertyNo}", GetByPropertyNo)
            .WithName(nameof(GetTangibleInventoryItemByPropertyNoQuery))
            .WithSummary("Get a tangible inventory item by property number")
            .Produces<TangibleInventoryItemDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.View);

    private static async Task<IResult> GetByPropertyNo(
        string propertyNo,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTangibleInventoryItemByPropertyNoQuery(propertyNo), cancellationToken);
        return TypedResults.Ok(result);
    }
}

