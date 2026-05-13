using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetReceivingReport;

public static class GetReceivingReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName("AssetRegister_GetReceivingReport")
            .WithSummary("Fetch a single Receiving Report (with item lines).")
            .Produces<ReceivingReportDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Receiving.View);

    private static async Task<IResult> Handle(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetReceivingReportQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}

