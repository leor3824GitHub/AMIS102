using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.DeletePropertyItemCatalog;

public static class DeletePropertyItemCatalogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Handle)
            .WithName(nameof(DeletePropertyItemCatalogCommand))
            .WithSummary("Delete a property item catalog entry")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Catalog.Delete);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeletePropertyItemCatalogCommand(id), ct);
        return TypedResults.NoContent();
    }
}

