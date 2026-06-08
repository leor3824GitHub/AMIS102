using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetMyAccountabilities;

public static class GetMyAccountabilitiesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/mine", Handle)
            .WithModuleName<GetMyAccountabilitiesQuery>()
            .WithSummary("Get my accountabilities (ICS / PAR issued to the current employee)")
            .Produces<PagedResponse<PropertyAccountabilitySummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.MyAccountability.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        AccountabilityType? type = null,
        AccountabilityStatus? status = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyAccountabilitiesQuery(
            keyword, type, status, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
