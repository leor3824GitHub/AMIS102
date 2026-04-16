using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.UpdatePropertyClassItem;

public static class UpdatePropertyClassItemEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/items/{id:guid}", async (
            Guid id,
            UpdatePropertyClassItemCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(UpdatePropertyClassItemCommand))
        .WithSummary("Update a property class item")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.Update);
}
