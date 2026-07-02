using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.CreateFundingSourceCode;

public static class CreateFundingSourceCodeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Create)
            .WithName(nameof(CreateFundingSourceCodeCommand))
            .WithSummary("Create funding source code")
            .Produces<FundingSourceCodeDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataPermissions.FundingSourceCodes.Create);

    private static async Task<IResult> Create(
        CreateFundingSourceCodeCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/funding-source-codes/{result.Id}", result);
    }
}
