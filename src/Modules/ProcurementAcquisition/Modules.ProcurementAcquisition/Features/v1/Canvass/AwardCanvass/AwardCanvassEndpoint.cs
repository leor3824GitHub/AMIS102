using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;

public static class AwardCanvassEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/award", AwardCanvass)
            .WithName(nameof(AwardCanvassCommand))
            .WithSummary("Award a canvass request to a supplier quotation")
            .Produces<CanvassRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.CanvassRequests.Award);

    private static async Task<IResult> AwardCanvass(
        Guid id,
        AwardCanvassCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { CanvassRequestId = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
