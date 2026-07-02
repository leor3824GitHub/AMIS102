using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodes;

public static class GetFundingSourceCodesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetList)
            .WithName(nameof(GetFundingSourceCodesQuery))
            .WithSummary("Get paginated list of funding source codes")
            .Produces<PagedResponseOfFundingSourceCodeDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.FundingSourceCodes.View);

    private static async Task<IResult> GetList(
        string? keyword = null,
        string? fundClusterCode = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundingSourceCodesQuery(keyword, fundClusterCode, pageNumber, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }
}
