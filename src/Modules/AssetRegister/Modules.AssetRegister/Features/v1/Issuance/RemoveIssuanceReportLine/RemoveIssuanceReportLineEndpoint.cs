using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.RemoveIssuanceReportLine;

public static class RemoveIssuanceReportLineEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}/lines/{lineId:guid}", Handle)
            .WithName(nameof(RemoveIssuanceReportLineCommand))
            .WithSummary("Remove a line from a draft issuance report")
            .Produces<PropertyIssuanceReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.Post);

    private static async Task<IResult> Handle(Guid id, Guid lineId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveIssuanceReportLineCommand(id, lineId), ct);
        return TypedResults.Ok(result);
    }
}

