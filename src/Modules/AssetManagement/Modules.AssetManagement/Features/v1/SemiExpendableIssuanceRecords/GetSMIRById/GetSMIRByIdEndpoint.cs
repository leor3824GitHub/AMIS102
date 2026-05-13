using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRById;

public static class GetSMIRByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetSMIRById)
            .WithName(nameof(GetSMIRByIdQuery))
            .WithSummary("Get a Semi-expendable Materials Issuance Record by ID")
            .Produces<SMIRDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableIssuanceRecords.View);

    private static async Task<IResult> GetSMIRById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSMIRByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

