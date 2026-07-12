using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.GetPropertyItemCatalogsByIds;

public static class GetPropertyItemCatalogsByIdsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/by-ids", Handle)
            .WithModuleName<GetPropertyItemCatalogsByIdsQuery>()
            .WithSummary("Get property item catalog entries for a set of ids (batch)")
            .Produces<IReadOnlyList<PropertyItemCatalogDto>>()
            .RequirePermission(AssetRegisterPermissions.Catalog.View);

    private static async Task<IResult> Handle(
        [FromBody] IReadOnlyCollection<Guid> ids,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPropertyItemCatalogsByIdsQuery(ids ?? []), ct);
        return TypedResults.Ok(result);
    }
}
