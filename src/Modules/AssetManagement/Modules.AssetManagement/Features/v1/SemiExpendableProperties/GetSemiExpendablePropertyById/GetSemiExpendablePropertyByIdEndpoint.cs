using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendablePropertyById;

public static class GetSemiExpendablePropertyByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetSemiExpendablePropertyById)
            .WithName(nameof(GetSemiExpendablePropertyByIdQuery))
            .WithSummary("Get semi-expendable property unit by ID")
            .Produces<SemiExpendablePropertyDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableProperties.View);

    private static async Task<IResult> GetSemiExpendablePropertyById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSemiExpendablePropertyByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
