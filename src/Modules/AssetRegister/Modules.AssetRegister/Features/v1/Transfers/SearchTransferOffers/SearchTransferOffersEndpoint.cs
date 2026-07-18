using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.SearchTransferOffers;

public static class SearchTransferOffersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<SearchTransferOffersQuery>()
            .WithSummary("Search inter-agency transfer offers (incoming and outgoing)")
            .Produces<PagedResponse<AssetTransferOfferSummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.Transfers.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        TransferOfferDirection? direction = null,
        TransferOfferStatus? status = null,
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchTransferOffersQuery(direction, status, keyword, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
