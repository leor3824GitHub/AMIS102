using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreateReceivingReport;

public static class CreateReceivingReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithModuleName<CreateReceivingReportCommand>()
            .WithSummary("Create a Receiving Report (PPERR or SMRR) and register assets.")
            .Produces<ReceivingReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Receiving.Create);

    private static async Task<IResult> Handle(
        CreateReceivingReportCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/receiving/{result.Id}", result);
    }
}

