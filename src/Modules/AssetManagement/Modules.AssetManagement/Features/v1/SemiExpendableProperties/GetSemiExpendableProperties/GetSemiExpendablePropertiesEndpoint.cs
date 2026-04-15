using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendableProperties;

public static class GetSemiExpendablePropertiesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetSemiExpendableProperties)
            .WithName(nameof(GetSemiExpendablePropertiesQuery))
            .WithSummary("Get paginated list of semi-expendable property units")
            .Produces<PagedSemiExpendablePropertiesResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableProperties.View);

    private static async Task<IResult> GetSemiExpendableProperties(
        string? keyword = null,
        Guid? semiExpendableItemId = null,
        AssetCategory? category = null,
        PropertyStatus? status = null,
        Guid? currentCustodianId = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSemiExpendablePropertiesQuery(keyword, semiExpendableItemId, category, status, currentCustodianId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
