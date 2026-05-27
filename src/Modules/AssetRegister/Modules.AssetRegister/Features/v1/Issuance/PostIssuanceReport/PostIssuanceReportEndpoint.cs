using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.PostIssuanceReport;

public static class PostIssuanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/post", Handle)
            .WithModuleName<PostIssuanceReportCommand>()
            .WithSummary("Post a draft issuance report (becomes immutable)")
            .Produces<PropertyIssuanceReportDto>()
            .RequirePermission(AssetRegisterPermissions.Issuance.Post);

    private static async Task<IResult> Handle(
        Guid id, PostIssuanceReportCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

