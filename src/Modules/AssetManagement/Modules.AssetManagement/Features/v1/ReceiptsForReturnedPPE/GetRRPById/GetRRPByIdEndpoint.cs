using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPById;

public static class GetRRPByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetRRPByIdQuery(id), ct)))
        .WithName(nameof(GetRRPByIdQuery))
        .WithSummary("Get a Receipt for Returned PPE by ID")
        .RequirePermission(AssetManagementModuleConstants.Permissions.ReceiptsForReturnedPPE.View);
}
