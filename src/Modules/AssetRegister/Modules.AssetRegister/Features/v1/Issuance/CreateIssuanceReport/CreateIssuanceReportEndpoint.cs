using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReport;

public static class CreateIssuanceReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithModuleName<CreateIssuanceReportCommand>()
            .WithSummary("Create a property issuance report (SMIR or PPEIR) — atomic transfer document")
            .Produces<PropertyIssuanceReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterPermissions.Issuance.Create);

    private static async Task<IResult> Handle(
        CreateIssuanceReportCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/issuance/{result.Id}", result);
    }
}
