using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.GetModesOfProcurement;

public static class GetModesOfProcurementEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetList)
            .WithName(nameof(GetModesOfProcurementQuery))
            .WithSummary("Get paginated list of modes of procurement")
            .Produces<PagedResponseOfModeOfProcurementDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.ModesOfProcurement.View);

    private static async Task<IResult> GetList(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetModesOfProcurementQuery(keyword, pageNumber, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }
}
