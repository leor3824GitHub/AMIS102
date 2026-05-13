using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Catalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.SetPropertyItemCatalogActivation;

public static class SetPropertyItemCatalogActivationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/activation", Handle)
            .WithName(nameof(SetPropertyItemCatalogActivationCommand))
            .WithSummary("Activate or deactivate a property item catalog entry")
            .Produces<PropertyItemCatalogDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Catalog.Update);

    private static async Task<IResult> Handle(
        Guid id, SetPropertyItemCatalogActivationCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.Id) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
