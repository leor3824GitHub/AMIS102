using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductImage;

public static class GetProductImageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/image", Handle)
            .WithName("Expendable_GetProductImage")
            .WithSummary("Get a product's photo (served inline; lazily loaded per list row). variant=thumb|full")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct, string? variant = null)
    {
        var imageVariant = string.Equals(variant, "thumb", StringComparison.OrdinalIgnoreCase)
            || string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase)
            ? ProductImageVariant.Thumbnail
            : ProductImageVariant.Full;

        var image = await mediator.Send(new GetProductImageQuery(id, imageVariant), ct);
        if (image is null)
            return TypedResults.NotFound();

        // Inline (no download filename) so the browser renders it as an <img>.
        return Results.File(image.Content, image.ContentType);
    }
}
