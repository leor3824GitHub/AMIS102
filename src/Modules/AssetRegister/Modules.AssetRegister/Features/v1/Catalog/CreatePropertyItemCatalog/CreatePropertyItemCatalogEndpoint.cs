using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;

public static class CreatePropertyItemCatalogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithModuleName<CreatePropertyItemCatalogCommand>()
            .WithSummary("Create a property item catalog entry")
            .Produces<PropertyItemCatalogDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetRegisterPermissions.Catalog.Create);

    private static async Task<IResult> Handle(
        CreatePropertyItemCatalogCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/catalog/{result.Id}", result);
    }
}

