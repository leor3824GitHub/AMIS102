using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEItems.GetPPEItemById;

public static class GetPPEItemByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPPEItemByIdQuery(id), ct)))
        .WithName(nameof(GetPPEItemByIdQuery))
        .WithSummary("Get a PPE item by ID")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEItems.View);
}
