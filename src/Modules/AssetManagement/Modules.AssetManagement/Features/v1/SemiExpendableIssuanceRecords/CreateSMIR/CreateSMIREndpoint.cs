using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;

public static class CreateSMIREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateSMIR)
            .WithName(nameof(CreateSMIRCommand))
            .WithSummary("Create a Semi-expendable Materials Issuance Record (SMIR) — inter-office transfer or disposal")
            .Produces<CreateSMIRResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableIssuanceRecords.Create);

    private static async Task<IResult> CreateSMIR(
        CreateSMIRCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/semi-expendable-issuance-records/{result.SMIRId}", result);
    }
}

