using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;

public static class UpsertOrganizationProfileEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/", Upsert)
            .WithName(nameof(UpsertOrganizationProfileCommand))
            .WithSummary("Create or update the organization profile for the current tenant")
            .Produces<OrganizationProfileDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.OrganizationProfile.Manage);

    private static async Task<IResult> Upsert(
        UpsertOrganizationProfileCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Ok(result);
    }
}

