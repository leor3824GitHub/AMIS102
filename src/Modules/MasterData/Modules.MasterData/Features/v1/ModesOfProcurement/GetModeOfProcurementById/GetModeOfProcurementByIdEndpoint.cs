using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.GetModeOfProcurementById;

public static class GetModeOfProcurementByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id}", GetById)
            .WithName(nameof(GetModeOfProcurementByIdQuery))
            .WithSummary("Get mode of procurement by ID")
            .Produces<ModeOfProcurementDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.ModesOfProcurement.View);

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetModeOfProcurementByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

