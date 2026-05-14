using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.ReturnAccountabilityLines;

public static class ReturnAccountabilityLinesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/returns", Handle)
            .WithModuleName<ReturnAccountabilityLinesCommand>()
            .WithSummary("Return one or more accountability lines (and free their assets)")
            .Produces<PropertyAccountabilityDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.Return);

    private static async Task<IResult> Handle(
        Guid id, ReturnAccountabilityLinesCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

