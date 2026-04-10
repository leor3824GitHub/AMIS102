using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.RegisterSemiExpendableProperty;

public static class RegisterSemiExpendablePropertyEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", RegisterSemiExpendableProperty)
            .WithName(nameof(RegisterSemiExpendablePropertyCommand))
            .WithSummary("Register a new semi-expendable property unit into inventory")
            .Produces<SemiExpendablePropertyDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableProperties.Create);

    private static async Task<IResult> RegisterSemiExpendableProperty(
        RegisterSemiExpendablePropertyCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/semi-expendable-properties/{result.Id}", result);
    }
}
