using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.DeleteFundingSourceCode;

public static class DeleteFundingSourceCodeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id}", Delete)
            .WithName(nameof(DeleteFundingSourceCodeCommand))
            .WithSummary("Delete funding source code")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.FundingSourceCodes.Delete);

    private static async Task<IResult> Delete(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteFundingSourceCodeCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
