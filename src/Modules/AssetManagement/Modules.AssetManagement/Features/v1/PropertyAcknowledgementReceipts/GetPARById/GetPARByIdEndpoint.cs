using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARById;

public static class GetPARByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPARByIdQuery(id), ct)))
        .WithName(nameof(GetPARByIdQuery))
        .WithSummary("Get a Property Acknowledgement Receipt by ID")
        .RequirePermission(AssetManagementPermissions.PropertyAcknowledgementReceipts.View);
}

