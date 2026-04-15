using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.RenewICS;

public static class RenewICSEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/renew", RenewICS)
            .WithName(nameof(RenewICSCommand))
            .WithSummary("Renew an active ICS, issuing a new ICS for the same custodian and property units")
            .Produces<RenewICSResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.InventoryCustodianSlips.Create);

    private static async Task<IResult> RenewICS(
        Guid id,
        RenewICSRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RenewICSCommand(id, request.NewICSNo, request.Date, request.IssuedFromEmployeeId);
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/inventory-custodian-slips/{result.NewICSId}", result);
    }
}

/// <summary>Request body for the RenewICS endpoint (id comes from route).</summary>
public sealed record RenewICSRequest(
    string NewICSNo,
    DateOnly Date,
    Guid? IssuedFromEmployeeId);
