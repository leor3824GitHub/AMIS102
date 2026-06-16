using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.DeleteLocation;

public static class DeleteLocationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Handle)
            .WithModuleName<DeleteLocationCommand>()
            .WithSummary("Delete (soft-delete) a location")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(AssetRegisterPermissions.Locations.Delete);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeleteLocationCommand(id), ct);
        return TypedResults.NoContent();
    }
}
