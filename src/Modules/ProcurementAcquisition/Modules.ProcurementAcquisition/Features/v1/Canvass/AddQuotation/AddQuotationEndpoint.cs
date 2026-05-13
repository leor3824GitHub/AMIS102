using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;

public static class AddQuotationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/quotations", AddQuotation)
            .WithName(nameof(AddQuotationCommand))
            .WithSummary("Add a supplier quotation to a canvass request")
            .Produces<CanvassQuotationDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.CanvassRequests.Update);

    private static async Task<IResult> AddQuotation(
        Guid id,
        AddQuotationCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { CanvassRequestId = id }, cancellationToken);
        return TypedResults.Created($"/api/v1/procurement/canvass-requests/{id}/quotations/{result.Id}", result);
    }
}

