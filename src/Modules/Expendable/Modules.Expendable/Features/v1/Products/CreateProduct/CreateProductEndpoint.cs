using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;

public static class CreateProductEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateProduct)
            .WithName(nameof(CreateProductCommand))
            .WithSummary("Create a new product")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.Create);

    private static async Task<IResult> CreateProduct(
        CreateProductCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/expendable/products/{result.Id}", result);
    }
}


