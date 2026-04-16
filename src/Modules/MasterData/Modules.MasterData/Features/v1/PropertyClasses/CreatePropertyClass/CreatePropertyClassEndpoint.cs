using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClass;

public static class CreatePropertyClassEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreatePropertyClassCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/master-data/property-classes/{id}", id);
        })
        .WithName(nameof(CreatePropertyClassCommand))
        .WithSummary("Create a property class (COA GAM Annex A classification)")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.Create);
}
