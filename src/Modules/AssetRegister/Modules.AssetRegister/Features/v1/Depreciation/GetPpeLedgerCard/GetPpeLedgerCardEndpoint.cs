using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Depreciation.GetPpeLedgerCard;

public static class GetPpeLedgerCardEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/ledger-card/{propertyNo}", Handle)
            .WithModuleName<GetPpeLedgerCardQuery>()
            .WithSummary("Generate the PPE Ledger Card (PPELC) with depreciation schedule")
            .Produces<PpeLedgerCardDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(string propertyNo, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPpeLedgerCardQuery(propertyNo), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
