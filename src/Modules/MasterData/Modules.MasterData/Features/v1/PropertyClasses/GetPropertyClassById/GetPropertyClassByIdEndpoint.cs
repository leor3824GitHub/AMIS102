using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassById;

public static class GetPropertyClassByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPropertyClassByIdQuery(id), ct);
            return result is null ? Results.NotFound() : TypedResults.Ok(result);
        })
        .WithName(nameof(GetPropertyClassByIdQuery))
        .WithSummary("Get a property class by ID with its items")
        .Produces<PropertyClassDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.View);
}
