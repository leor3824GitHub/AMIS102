using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRById;

public static class GetSMRRByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetSMRRByIdQuery(id), ct)))
        .WithName(nameof(GetSMRRByIdQuery))
        .WithSummary("Get a Supplies and Materials Receiving Report by ID, including its line items")
        .RequirePermission(AssetManagementModuleConstants.Permissions.ReceivingReports.View);
}
