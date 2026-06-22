using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetReceivedPropertyNumbers;

public static class GetReceivedPropertyNumbersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/received-property-numbers", Handle)
            .WithModuleName<GetReceivedPropertyNumbersQuery>()
            .WithSummary("Return which candidate property numbers are already received (SMRR / PPERR).")
            .Produces<IReadOnlyCollection<string>>()
            .RequirePermission(AssetRegisterPermissions.Receiving.View);

    private static async Task<IResult> Handle(
        GetReceivedPropertyNumbersQuery request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return TypedResults.Ok(result);
    }
}
