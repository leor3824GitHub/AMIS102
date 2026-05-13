using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.DeleteSupplier;

public static class DeleteSupplierEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id}", DeleteSupplier)
            .WithName(nameof(DeleteSupplierCommand))
            .WithSummary("Delete supplier")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Suppliers.Delete);

    private static async Task<IResult> DeleteSupplier(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteSupplierCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}

