using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.GetIssuanceReport;

public static class GetIssuanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetIssuanceReportQuery))
            .WithSummary("Get an issuance report by id")
            .Produces<PropertyIssuanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetIssuanceReportQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
