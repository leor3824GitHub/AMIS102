using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;

public static class CreateCanvassRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateCanvassRequest)
            .WithName($"Procurement.{nameof(CreateCanvassRequestCommand)}")
            .WithSummary("Create a canvass/RFQ request from an approved purchase request")
            .Produces<CanvassRequestDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.CanvassRequests.Create);

    private static async Task<IResult> CreateCanvassRequest(
        CreateCanvassRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/procurement/canvass-requests/{result.Id}", result);
    }
}

