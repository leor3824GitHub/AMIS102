using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.GetTransferDestinations;

public static class GetTransferDestinationsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/destinations", Handle)
            .WithModuleName<GetTransferDestinationsQuery>()
            .WithSummary("List agencies this tenant can transfer property to")
            .Produces<IReadOnlyList<TransferDestinationDto>>()
            .RequirePermission(AssetRegisterPermissions.Transfers.Offer);

    private static async Task<IResult> Handle(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTransferDestinationsQuery(), ct);
        return TypedResults.Ok(result);
    }
}
