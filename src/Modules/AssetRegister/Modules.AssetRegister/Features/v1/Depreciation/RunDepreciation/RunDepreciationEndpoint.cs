using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Depreciation.RunDepreciation;

public static class RunDepreciationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/run", Handle)
            .WithModuleName<RunDepreciationCommand>()
            .WithSummary("Post monthly straight-line depreciation for all PPE assets up to a period")
            .Produces<RunDepreciationResultDto>()
            .RequirePermission(AssetRegisterPermissions.Assets.Update);

    private static async Task<IResult> Handle(RunDepreciationCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
