using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Categories.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id}", UpdateCategory)
            .WithName(nameof(UpdateCategoryCommand))
            .WithSummary("Update category")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Categories.Update);

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateCategoryCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.NoContent();
    }
}

