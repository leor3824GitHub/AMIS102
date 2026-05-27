using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.GetSupplierById;

public static class GetSupplierByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id}", GetSupplierById)
            .WithName(nameof(GetSupplierByIdQuery))
            .WithSummary("Get supplier by ID")
            .Produces<SupplierDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.Suppliers.View);

    private static async Task<IResult> GetSupplierById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSupplierByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

