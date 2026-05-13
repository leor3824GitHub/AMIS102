using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;

public static class CreateSupplierEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateSupplier)
            .WithName(nameof(CreateSupplierCommand))
            .WithSummary("Create supplier")
            .Produces<SupplierDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataModuleConstants.Permissions.Suppliers.Create);

    private static async Task<IResult> CreateSupplier(
        CreateSupplierCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/suppliers/{result.Id}", result);
    }
}

