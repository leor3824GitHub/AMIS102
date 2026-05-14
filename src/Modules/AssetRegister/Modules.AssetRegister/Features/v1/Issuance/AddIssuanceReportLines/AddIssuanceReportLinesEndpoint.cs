using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.AddIssuanceReportLines;

public static class AddIssuanceReportLinesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/lines", Handle)
            .WithModuleName<AddIssuanceReportLinesCommand>()
            .WithSummary("Snapshot accountability lines into a draft issuance report")
            .Produces<PropertyIssuanceReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.Post);

    private static async Task<IResult> Handle(
        Guid id, AddIssuanceReportLinesCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

