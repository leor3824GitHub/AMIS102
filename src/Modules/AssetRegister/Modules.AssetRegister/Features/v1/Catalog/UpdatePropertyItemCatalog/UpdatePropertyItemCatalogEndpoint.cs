using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.UpdatePropertyItemCatalog;

public static class UpdatePropertyItemCatalogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithModuleName<UpdatePropertyItemCatalogCommand>()
            .WithSummary("Update a property item catalog entry")
            .Produces<PropertyItemCatalogDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Catalog.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdatePropertyItemCatalogCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.Id)
            return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

