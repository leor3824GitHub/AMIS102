using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.SearchPropertyItemCatalogs;

public static class SearchPropertyItemCatalogsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchPropertyItemCatalogsQuery))
            .WithSummary("Search property item catalog entries")
            .Produces<PagedResponse<PropertyItemCatalogDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Catalog.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchPropertyItemCatalogsQuery(keyword, isActive, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
