using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetImage;

public static class GetAssetImageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/image", Handle)
            .WithModuleName<GetAssetImageQuery>()
            .WithSummary("Get an asset's photo (served inline; lazily loaded per list row). variant=thumb|full")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct, string? variant = null)
    {
        var imageVariant = string.Equals(variant, "thumb", StringComparison.OrdinalIgnoreCase)
            || string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase)
            ? AssetImageVariant.Thumbnail
            : AssetImageVariant.Full;

        var image = await mediator.Send(new GetAssetImageQuery(id, imageVariant), ct);
        if (image is null)
            return TypedResults.NotFound();

        // Inline (no download filename) so the browser/CollectionView renders it as an <img>.
        return Results.File(image.Content, image.ContentType);
    }
}
