using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodeById;

public static class GetFundingSourceCodeByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id}", GetById)
            .WithName(nameof(GetFundingSourceCodeByIdQuery))
            .WithSummary("Get funding source code by ID")
            .Produces<FundingSourceCodeDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.FundingSourceCodes.View);

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFundingSourceCodeByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
