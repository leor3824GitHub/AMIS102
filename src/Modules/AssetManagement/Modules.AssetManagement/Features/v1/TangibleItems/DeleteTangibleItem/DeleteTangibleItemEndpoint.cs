using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.DeleteTangibleItem;

public static class DeleteTangibleItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", DeleteTangibleItem)
            .WithName(nameof(DeleteTangibleItemCommand))
            .WithSummary("Delete (soft-delete) a tangible item")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementPermissions.TangibleItems.Delete);

    private static async Task<IResult> DeleteTangibleItem(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTangibleItemCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}

