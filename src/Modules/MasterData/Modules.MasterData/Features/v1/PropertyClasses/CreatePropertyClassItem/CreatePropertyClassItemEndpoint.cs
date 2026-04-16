using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClassItem;

public static class CreatePropertyClassItemEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{propertyClassId:guid}/items", async (
            Guid propertyClassId,
            CreatePropertyClassItemCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(command with { PropertyClassId = propertyClassId }, ct);
            return TypedResults.Created($"/api/v1/master-data/property-classes/{propertyClassId}/items/{id}", id);
        })
        .WithName(nameof(CreatePropertyClassItemCommand))
        .WithSummary("Add a category item to a property class")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.Create);
}
