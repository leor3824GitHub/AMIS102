using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRById;

public static class GetPPEIRByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPPEIRByIdQuery(id), ct)))
        .WithName(nameof(GetPPEIRByIdQuery))
        .WithSummary("Get a PPE Issuance Report by ID")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEIssuanceReports.View);
}

