using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRById;

public static class GetPPERRByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPPERRByIdQuery(id), ct)))
        .WithName(nameof(GetPPERRByIdQuery))
        .WithSummary("Get a PPE Receiving Report by ID")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEReceivingReports.View);
}
