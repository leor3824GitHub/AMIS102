using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

public static class GetRegSPIEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/reg-spi", GetRegSPI)
            .WithName(nameof(GetRegSPIQuery))
            .WithSummary("Registry of Semi-Expendable Property Issued (RegSPI) — all ICS lines for an employee")
            .Produces<PagedRegSPIResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reports.View);

    private static async Task<IResult> GetRegSPI(
        [AsParameters] GetRegSPIQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
