using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReportDraft;

public static class CreateIssuanceReportDraftEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName(nameof(CreateIssuanceReportDraftCommand))
            .WithSummary("Create a draft issuance report (RSPI/PPEIR)")
            .Produces<PropertyIssuanceReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.Post);

    private static async Task<IResult> Handle(
        CreateIssuanceReportDraftCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/issuance/{result.Id}", result);
    }
}

